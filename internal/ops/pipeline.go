package ops

import (
	"bytes"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strconv"
	"strings"

	"github.com/tinyscr/etsu/internal/app"
)

func Process(files []string, logLines *[]string, stage2 string, apply func(tempFile, orig string) error) error {
	if err := os.MkdirAll(app.TempDir(), 0o755); err != nil {
		app.WriteLine("Cannot create temp workspace: "+err.Error(), app.Red)
		return err
	}
	tempDir, err := os.MkdirTemp(app.TempDir(), "etsu_")
	if err != nil {
		app.WriteLine("Cannot create temp workspace: "+err.Error(), app.Red)
		return err
	}
	defer os.RemoveAll(tempDir)

	fileMap := make(map[string]string, len(files))

	app.WriteStep(1, 5, "Copying files to temp workspace...", "")
	*logLines = append(*logLines, "", "[1/5] Copying files to temp workspace")

	for i, file := range files {
		temp := filepath.Join(tempDir, strconv.Itoa(i)+filepath.Ext(file))
		if err := app.CopyFile(file, temp); err != nil {
			return abort(logLines, "Copy failed for "+filepath.Base(file)+" : "+err.Error(), err)
		}

		origInfo, err := os.Stat(file)
		if err != nil {
			return abort(logLines, "Cannot stat "+filepath.Base(file)+" : "+err.Error(), err)
		}
		copyInfo, err := os.Stat(temp)
		if err != nil {
			return abort(logLines, "Cannot stat temp copy of "+filepath.Base(file)+" : "+err.Error(), err)
		}
		if origInfo.Size() != copyInfo.Size() {
			msg := fmt.Sprintf("Size mismatch for %s (orig: %d, copy: %d)", filepath.Base(file), origInfo.Size(), copyInfo.Size())
			return abort(logLines, msg, fmt.Errorf("%s", msg))
		}

		fileMap[file] = temp
	}
	*logLines = append(*logLines, "All copies verified OK")

	app.WriteStep(2, 5, stage2, "")
	for _, file := range files {
		if err := apply(fileMap[file], file); err != nil {
			return err
		}
	}

	app.WriteStep(3, 5, "Verifying files...", "")
	*logLines = append(*logLines, "", "[3/5] Verifying files")
	for _, file := range files {
		temp := fileMap[file]
		info, err := os.Stat(temp)
		if err != nil {
			return abort(logLines, "Temp file missing: "+filepath.Base(file), err)
		}
		if info.Size() == 0 {
			return abort(logLines, "Temp file is empty: "+filepath.Base(file), fmt.Errorf("empty file"))
		}
		fh, err := os.Open(temp)
		if err != nil {
			return abort(logLines, "Temp file unreadable: "+filepath.Base(file), err)
		}
		fh.Close()
	}
	*logLines = append(*logLines, "All processed files verified OK")

	app.WriteStep(4, 5, "Replacing originals...", "")
	*logLines = append(*logLines, "", "[4/5] Replacing originals")

	var bakFiles []string
	done := false
	defer func() {
		if done {
			return
		}
		for _, bak := range bakFiles {
			orig := strings.TrimSuffix(bak, ".bak")
			os.Remove(orig)
			os.Rename(bak, orig)
		}
	}()

	for _, file := range files {
		temp := fileMap[file]
		bak := file + ".bak"

		if _, err := os.Stat(bak); err == nil {
			return abort(logLines, "Backup already exists: "+bak, fmt.Errorf("backup exists"))
		}

		if err := os.Rename(file, bak); err != nil {
			return abort(logLines, "Could not rename original to .bak: "+filepath.Base(file)+" : "+err.Error(), err)
		}
		bakFiles = append(bakFiles, bak)

		if err := os.Rename(temp, file); err != nil {
			return abort(logLines, "Could not move temp file to original path: "+filepath.Base(file)+" : "+err.Error(), err)
		}

		info, err := os.Stat(file)
		if err != nil || info.Size() == 0 {
			return abort(logLines, "Final file missing or empty after swap: "+filepath.Base(file), fmt.Errorf("missing or empty"))
		}
		*logLines = append(*logLines, "  Swapped OK: "+file)
	}

	for _, bak := range bakFiles {
		os.Remove(bak)
	}
	done = true
	return nil
}

func abort(logLines *[]string, msg string, err error) error {
	app.WriteLine("[ABORT] "+msg, app.Red)
	*logLines = append(*logLines, "[ABORT] "+msg)
	return err
}

func runExif(args ...string) (string, error) {
	cmd := exec.Command(app.ExifPath, args...)
	var stdout, stderr bytes.Buffer
	cmd.Stdout = &stdout
	cmd.Stderr = &stderr
	err := cmd.Run()

	out := stdout.String()
	if stderr.Len() > 0 {
		out += stderr.String()
	}
	return out, err
}
