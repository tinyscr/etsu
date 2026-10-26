package app

import (
	"bufio"
	"errors"
	"fmt"
	"io"
	"os"
	"os/exec"
	"path/filepath"
	"sort"
	"strings"
	"time"

	"github.com/tinyscr/etsu/internal/config"
)

var (
	ExifPath    string
	ExifVersion string
	BaseDir     string
	cfg         *config.File
)

func Init() {
	initConsole()
	if exe, err := os.Executable(); err == nil {
		BaseDir = filepath.Dir(exe)
	}
	cfg = config.Load(ConfigPath())
}

func appData() string {
	if v := os.Getenv("APPDATA"); v != "" {
		return v
	}
	d, _ := os.UserConfigDir()
	return d
}

func localAppData() string {
	if v := os.Getenv("LOCALAPPDATA"); v != "" {
		return v
	}
	d, _ := os.UserCacheDir()
	return d
}

func ConfigDir() string  { return filepath.Join(appData(), "etsu") }
func ConfigPath() string { return filepath.Join(ConfigDir(), ".etsu") }
func LogDir() string     { return filepath.Join(localAppData(), "etsu", "logs", "etsu") }
func TempDir() string    { return filepath.Join(localAppData(), "etsu", "tmp") }

func GeneralString(key, def string) string { return cfg.String("General", key, def) }
func LogEnabled(tool string) bool          { return cfg.Bool(tool, "log", tool != "read") }
func LogCount(tool string) int             { return cfg.Int(tool, "logCount", 10) }

func FindExifTool() bool {
	configured := GeneralString("exiftoolPATH", "null")

	if !strings.EqualFold(configured, "null") {
		if strings.Contains(configured, `"`) {
			fmt.Fprintln(os.Stderr, "exiftoolPATH contains illegal quote character.")
			return false
		}
		if _, err := os.Stat(configured); err != nil {
			fmt.Fprintln(os.Stderr, "exiftool not found at configured path: "+configured)
			return false
		}
		ExifPath = configured
		ExifVersion = strings.TrimSpace(RunTool(ExifPath, "-ver"))
		return true
	}

	if path, err := exec.LookPath("exiftool.exe"); err == nil {
		ExifPath = path
		ExifVersion = strings.TrimSpace(RunTool(path, "-ver"))
		return true
	}

	local := filepath.Join(BaseDir, "exiftool.exe")
	if _, err := os.Stat(local); err == nil {
		ExifPath = local
		ExifVersion = strings.TrimSpace(RunTool(local, "-ver"))
		return true
	}

	fmt.Fprintln(os.Stderr, "exiftool.exe not found on PATH or in "+BaseDir)
	fmt.Fprintln(os.Stderr, "Install ExifTool or set exiftoolPATH in "+ConfigPath()+".")
	return false
}

func RunTool(exe string, args ...string) string {
	out, err := exec.Command(exe, args...).Output()
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

type Color int

const (
	Gray Color = iota
	DarkGray
	Red
	Green
)

func WriteHeader(mode string) {
	fmt.Println()
	fmt.Println(" ┌─ 🐾 ETSU   |   " + mode + "   |   Exiftool: v" + ExifVersion)
	fmt.Println(" " + strings.Repeat("─", 46))
	fmt.Println()
}

func WriteLine(text string, color ...Color) {
	c := Gray
	if len(color) > 0 {
		c = color[0]
	}
	if vtEnabled {
		fmt.Print(colorCode(c))
	}
	fmt.Println("  " + text)
	if vtEnabled {
		fmt.Print("\x1b[0m")
	}
}

func WriteStep(num, total int, label, status string) {
	WriteLine(fmt.Sprintf("[%d/%d] - %s%s", num, total, label, status), DarkGray)
}

func WriteSep() {
	fmt.Println()
	WriteLine(strings.Repeat("─", 46), DarkGray)
	fmt.Println()
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

func WaitExit() int {
	WriteLine("Press enter to exit.", DarkGray)
	bufio.NewReader(os.Stdin).ReadString('\n')
	return 0
}

func CopyFile(src, dst string) error {
	in, err := os.Open(src)
	if err != nil {
		return err
	}
	defer in.Close()

	out, err := os.Create(dst)
	if err != nil {
		return err
	}
	if _, err := io.Copy(out, in); err != nil {
		out.Close()
		return err
	}
	return out.Close()
}
