package app

import (
	"path/filepath"
	"testing"
)

func TestPathsUseInstallDir(t *testing.T) {
	old := BaseDir
	BaseDir = t.TempDir()
	defer func() { BaseDir = old }()

	if got, want := ConfigPath(), filepath.Join(BaseDir, "config.ini"); got != want {
		t.Fatalf("ConfigPath() = %q, want %q", got, want)
	}
	if got, want := LogDir(), filepath.Join(BaseDir, "logs", "etsu"); got != want {
		t.Fatalf("LogDir() = %q, want %q", got, want)
	}
	if got, want := TempDir(), filepath.Join(BaseDir, "tmp"); got != want {
		t.Fatalf("TempDir() = %q, want %q", got, want)
	}
}
