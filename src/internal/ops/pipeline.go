package ops

import (
	"bytes"
	"context"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"syscall"

	"github.com/tinyscr/etsu/internal/app"
)

// Reporter receives progress and raw log lines while an operation runs.
type Reporter interface {
	Progress(percent int, label string)
	Log(line string)
}

func Process(ctx context.Context, file string, rep Reporter, stage2 string, apply func(tempFile, orig string) error) error {
	if err := ctx.Err(); err != nil {
		return err
	}
	tempDir, err := os.MkdirTemp(filepath.Dir(file), ".etsu_")
	if err != nil {
		return abort(rep, "Cannot create temp workspace: "+err.Error(), err)
	}
	defer os.RemoveAll(tempDir)

	temp := filepath.Join(tempDir, "file"+filepath.Ext(file))

	rep.Progress(0, "Copying to temp workspace")
	rep.Log("[1/5] Copying to temp workspace")

	if err := app.CopyFile(ctx, file, temp); err != nil {
		return abort(rep, "Copy failed for "+filepath.Base(file)+": "+err.Error(), err)
	}

	origInfo, err := os.Stat(file)
	if err != nil {
		return abort(rep, "Cannot stat "+filepath.Base(file)+": "+err.Error(), err)
	}
	copyInfo, err := os.Stat(temp)
	if err != nil {
		return abort(rep, "Cannot stat temp copy of "+filepath.Base(file)+": "+err.Error(), err)
	}
	if origInfo.Size() != copyInfo.Size() {
		msg := fmt.Sprintf("Size mismatch for %s (orig: %d, copy: %d)", filepath.Base(file), origInfo.Size(), copyInfo.Size())
		return abort(rep, msg, fmt.Errorf("%s", msg))
	}
	rep.Log("Copy verified OK")

	if err := ctx.Err(); err != nil {
		return err
	}

	rep.Progress(20, stage2)
	rep.Log("[2/5] " + stage2)
	if err := apply(temp, file); err != nil {
		return err
	}

	if err := ctx.Err(); err != nil {
		return err
	}

	rep.Progress(40, "Verifying")
	rep.Log("[3/5] Verifying")
	info, err := os.Stat(temp)
	if err != nil {
		return abort(rep, "Temp file missing: "+filepath.Base(file), err)
	}
	if info.Size() == 0 {
		return abort(rep, "Temp file is empty: "+filepath.Base(file), fmt.Errorf("empty file"))
	}
	fh, err := os.Open(temp)
	if err != nil {
		return abort(rep, "Temp file unreadable: "+filepath.Base(file), err)
	}
	fh.Close()
	rep.Log("Processed file verified OK")

	if err := ctx.Err(); err != nil {
		return err
	}

	rep.Progress(60, "Replacing originals")
	rep.Log("[4/5] Replacing originals")

	bak := file + ".bak"
	if _, err := os.Stat(bak); err == nil {
		return abort(rep, "Backup already exists: "+bak, fmt.Errorf("backup exists"))
	}

	swapped := false
	backedUp := false
	defer func() {
		if swapped || !backedUp {
			return
		}
		if err := os.Remove(file); err != nil && !os.IsNotExist(err) {
			rep.Log("[ROLLBACK] Could not remove " + filepath.Base(file) + ": " + err.Error())
			return
		}
		if err := os.Rename(bak, file); err != nil {
			rep.Log("[ROLLBACK] Could not restore " + filepath.Base(file) + ": " + err.Error())
			return
		}
		rep.Log("[ROLLBACK] Restored " + filepath.Base(file))
	}()

	if err := os.Rename(file, bak); err != nil {
		return abort(rep, "Could not rename original to .bak: "+filepath.Base(file)+": "+err.Error(), err)
	}
	backedUp = true
	if err := os.Rename(temp, file); err != nil {
		return abort(rep, "Could not move temp file to original path: "+filepath.Base(file)+": "+err.Error(), err)
	}

	info, err = os.Stat(file)
	if err != nil || info.Size() == 0 {
		return abort(rep, "Final file missing or empty after swap: "+filepath.Base(file), fmt.Errorf("missing or empty"))
	}
	rep.Log("Swapped OK: 1 file")

	if err := ctx.Err(); err != nil {
		return err
	}

	rep.Progress(80, "Finishing")
	rep.Log("[5/5] Finishing")
	if err := os.Remove(bak); err != nil {
		return abort(rep, "Could not remove backup: "+filepath.Base(bak)+": "+err.Error(), err)
	}
	swapped = true

	rep.Progress(100, "Done")
	return nil
}

func abort(rep Reporter, msg string, err error) error {
	rep.Log("[ABORT] " + msg)
	return err
}

func runExif(ctx context.Context, args ...string) (string, error) {
	cmd := exec.CommandContext(ctx, app.ExifPath, args...)
	cmd.SysProcAttr = &syscall.SysProcAttr{HideWindow: true}
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

func logExifOutput(rep Reporter, out string) {
	for _, line := range app.SplitLines(out) {
		rep.Log("  " + line)
	}
}
