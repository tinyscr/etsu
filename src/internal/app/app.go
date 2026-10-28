package app

import (
	"context"
	"errors"
	"fmt"
	"io"
	"os"
	"os/exec"
	"path/filepath"
	"sort"
	"strings"
	"syscall"
	"time"

	"github.com/tinyscr/etsu/internal/config"
)

var (
	ExifPath    string
	ExifVersion string
	BaseDir     string
	cfg         *config.File
)

func Init() error {
	initConsole()
	if exe, err := os.Executable(); err == nil {
		BaseDir = filepath.Dir(exe)
	}
	if BaseDir == "" {
		BaseDir = "."
	}

	if err := ensureWritable(TempDir()); err != nil {
		return err
	}

	cfg = config.Load(ConfigPath())
	return nil
}

func ensureWritable(dir string) error {
	if err := os.MkdirAll(dir, 0o755); err != nil {
		return fmt.Errorf("install directory is not writable: %s", BaseDir)
	}

	probe, err := os.CreateTemp(dir, ".write-test-")
	if err != nil {
		return fmt.Errorf("install directory is not writable: %s", BaseDir)
	}
	name := probe.Name()
	_ = probe.Close()
	_ = os.Remove(name)
	return nil
}

func ConfigPath() string { return filepath.Join(BaseDir, "config.ini") }
func LogDir() string     { return filepath.Join(BaseDir, "logs", "etsu") }
func TempDir() string    { return filepath.Join(BaseDir, "tmp") }

func GeneralString(key, def string) string { return cfg.String("General", key, def) }
func LogEnabled(tool string) bool          { return cfg.Bool(tool, "log", tool != "read") }
func LogCount(tool string) int             { return cfg.Int(tool, "logCount", 10) }

func FindExifTool() error {
	configured := GeneralString("exiftoolPATH", "null")

	if !strings.EqualFold(configured, "null") {
		if strings.Contains(configured, `"`) {
			return errors.New("exiftoolPATH contains an illegal quote character")
		}
		if _, err := os.Stat(configured); err != nil {
			return fmt.Errorf("ExifTool was not found at the configured path:\n%s", configured)
		}
		ExifPath = configured
		ExifVersion = strings.TrimSpace(RunTool(ExifPath, "-ver"))
		return nil
	}

	if path, err := exec.LookPath("exiftool.exe"); err == nil {
		ExifPath = path
		ExifVersion = strings.TrimSpace(RunTool(path, "-ver"))
		return nil
	}

	local := filepath.Join(BaseDir, "exiftool.exe")
	if _, err := os.Stat(local); err == nil {
		ExifPath = local
		ExifVersion = strings.TrimSpace(RunTool(local, "-ver"))
		return nil
	}

	return fmt.Errorf("ExifTool was not found.\n\nInstall ExifTool or set exiftoolPATH in:\n%s", ConfigPath())
}

func RunTool(exe string, args ...string) string {
	cmd := exec.Command(exe, args...)
	cmd.SysProcAttr = &syscall.SysProcAttr{HideWindow: true}
	out, err := cmd.Output()
	if err != nil {
		var exitErr *exec.ExitError
		if errors.As(err, &exitErr) {
			return string(out) + string(exitErr.Stderr)
		}
	}
	return string(out)
}

func SplitLines(s string) []string {
	return strings.FieldsFunc(s, func(r rune) bool { return r == '\r' || r == '\n' })
}

func WriteLog(prefix, outcome string, lines []string) {
	if !LogEnabled(prefix) {
		return
	}

	dir := LogDir()
	if err := os.MkdirAll(dir, 0o755); err != nil {
		return
	}

	now := time.Now()
	path := filepath.Join(dir, prefix+"_"+now.Format("20060102_150405")+".log")

	var b strings.Builder
	b.WriteString("ExifTool " + prefix + " Log\n")
	b.WriteString("Timestamp : " + now.Format("2006-01-02 15:04:05") + "\n")
	b.WriteString("Outcome   : " + outcome + "\n")
	b.WriteString("----------------------------------------\n\n")
	for _, line := range lines {
		b.WriteString(line + "\n")
	}
	if err := os.WriteFile(path, []byte(b.String()), 0o644); err != nil {
		return
	}

	files, _ := filepath.Glob(filepath.Join(dir, prefix+"_*.log"))
	sort.Strings(files)
	keep := LogCount(prefix)
	for len(files) > keep {
		os.Remove(files[0])
		files = files[1:]
	}
}

func CopyFile(ctx context.Context, src, dst string) error {
	in, err := os.Open(src)
	if err != nil {
		return err
	}
	defer in.Close()

	out, err := os.Create(dst)
	if err != nil {
		return err
	}

	buf := make([]byte, 1<<20)
	for {
		if err := ctx.Err(); err != nil {
			out.Close()
			return err
		}
		n, readErr := in.Read(buf)
		if n > 0 {
			if _, err := out.Write(buf[:n]); err != nil {
				out.Close()
				return err
			}
		}
		if readErr == io.EOF {
			break
		}
		if readErr != nil {
			out.Close()
			return readErr
		}
	}
	return out.Close()
}
