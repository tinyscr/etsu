package main

import "testing"

func TestParseArgs(t *testing.T) {
	tests := []struct {
		name       string
		args       []string
		wantAction string
		wantFile   string
		wantOK     bool
	}{
		{name: "read", args: []string{"read", `C:\a.jpg`}, wantAction: "read", wantFile: `C:\a.jpg`, wantOK: true},
		{name: "uppercase", args: []string{"CLEAN", `C:\a.jpg`}, wantAction: "clean", wantFile: `C:\a.jpg`, wantOK: true},
		{name: "date", args: []string{"date", `C:\a.jpg`}, wantAction: "date", wantFile: `C:\a.jpg`, wantOK: true},
		{name: "missing file", args: []string{"read"}, wantOK: false},
		{name: "extra arg", args: []string{"read", "a.jpg", "b.jpg"}, wantOK: false},
		{name: "unknown action", args: []string{"delete", "a.jpg"}, wantOK: false},
		{name: "empty file", args: []string{"read", ""}, wantOK: false},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			action, file, ok := parseArgs(tt.args)
			if action != tt.wantAction || file != tt.wantFile || ok != tt.wantOK {
				t.Fatalf("parseArgs(%q) = (%q, %q, %v), want (%q, %q, %v)",
					tt.args, action, file, ok, tt.wantAction, tt.wantFile, tt.wantOK)
			}
		})
	}
}
