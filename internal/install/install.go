package install

import (
	_ "embed"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
	"syscall"

	"github.com/tinyscr/etsu/internal/app"
)

//go:embed default.etsu
var defaultConfig string

func Install() int {
	if err := os.MkdirAll(app.ConfigDir(), 0o755); err != nil {
		app.WriteLine("Could not create config directory: "+err.Error(), app.Red)
		return 1
	}
	if err := seedConfig(); err != nil {
		app.WriteLine("Could not write config: "+err.Error(), app.Red)
		return 1
	}

	if !app.FindExifTool() {
		return 1
	}

	cur, err := os.Executable()
	if err != nil {
		app.WriteLine("Could not locate the running executable: "+err.Error(), app.Red)
		return 1
	}
	cur, _ = filepath.Abs(cur)
	target := filepath.Join(app.InstallDir(), "etsu.exe")
	target, _ = filepath.Abs(target)

	if !strings.EqualFold(cur, target) {
		if err := os.MkdirAll(app.InstallDir(), 0o755); err != nil {
			app.WriteLine("Could not create install directory: "+err.Error(), app.Red)
			return 1
		}
		if err := app.CopyFile(cur, target); err != nil {
			app.WriteLine("Could not copy etsu.exe: "+err.Error(), app.Red)
			return 1
		}
		app.WriteLine("Installed to "+target, app.DarkGray)
	}

	if err := registerMenu(target); err != nil {
		app.WriteLine("Could not register the context menu: "+err.Error(), app.Red)
		return 1
	}

	app.WriteLine("Registered etsu Explorer context menu.", app.Green)
	app.WriteLine("On Windows 11 it appears under Show more options.", app.DarkGray)
	return 0
}

func Uninstall(purge bool) int {
	removed, err := unregisterMenu()
	if err != nil {
		app.WriteLine("Could not remove the context menu: "+err.Error(), app.Red)
		return 1
	}
	if removed {
		app.WriteLine("Removed etsu Explorer context menu.", app.Green)
	} else {
		app.WriteLine("etsu context menu is not installed.", app.DarkGray)
	}

	if purge {
		os.RemoveAll(app.ConfigDir())
		os.RemoveAll(app.DataDir())
		app.WriteLine("Removed config, logs, and temp files.", app.Green)
	}

	cur, err := os.Executable()
	if err != nil {
		return 0
	}
	cur, _ = filepath.Abs(cur)
	installDir := app.InstallDir()

	if isInside(cur, installDir) {
		if err := scheduleRemoveDir(installDir); err != nil {
			app.WriteLine("Could not schedule removal of "+installDir+": "+err.Error(), app.Red)
			return 1
		}
		app.WriteLine("Install directory will be removed after exit.", app.DarkGray)
	} else if _, err := os.Stat(installDir); err == nil {
		if err := os.RemoveAll(installDir); err != nil {
			app.WriteLine("Could not remove "+installDir+": "+err.Error(), app.Red)
			return 1
		}
		app.WriteLine("Removed "+installDir, app.Green)
	}

	return 0
}

func seedConfig() error {
	path := app.ConfigPath()
	if _, err := os.Stat(path); err == nil {
		return nil
	}
	return os.WriteFile(path, []byte(defaultConfig), 0o644)
}

func isInside(path, dir string) bool {
	rel, err := filepath.Rel(dir, path)
	if err != nil {
		return false
	}
	return rel != ".." && !strings.HasPrefix(rel, `..\`) && !filepath.IsAbs(rel)
}

func scheduleRemoveDir(dir string) error {
	script := filepath.Join(os.TempDir(), fmt.Sprintf("etsu-uninstall-%d.cmd", os.Getpid()))
	content := "@echo off\r\n" +
		"ping 127.0.0.1 -n 3 >nul\r\n" +
		"rmdir /s /q \"" + dir + "\"\r\n" +
		"del \"%~f0\"\r\n"
	if err := os.WriteFile(script, []byte(content), 0o600); err != nil {
		return err
	}

	cmd := exec.Command("cmd", "/c", script)
	cmd.Dir = os.TempDir()
	cmd.SysProcAttr = &syscall.SysProcAttr{HideWindow: true}
	if err := cmd.Start(); err != nil {
		os.Remove(script)
		return err
	}
	return nil
}
