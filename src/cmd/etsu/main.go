package main

import (
	"context"
	"errors"
	"os"
	"runtime"
	"strings"
	"time"

	"github.com/tinyscr/etsu/internal/app"
	"github.com/tinyscr/etsu/internal/ops"
	"github.com/tinyscr/etsu/internal/ui"
)

func main() {
	os.Exit(run(os.Args[1:]))
}

func run(args []string) int {
	// Win32 windows and their message queues are tied to one OS thread.
	runtime.LockOSThread()
	defer runtime.UnlockOSThread()

	if err := app.Init(); err != nil {
		ui.ShowError("ETSU", err.Error())
		return 1
	}

	action, file, ok := parseArgs(args)
	if !ok {
		ui.ShowInfo("ETSU", "Usage: etsu read|clean|date <file>\n\nETSU normally runs from the Explorer context menu.")
		return 2
	}

	if err := app.FindExifTool(); err != nil {
		ui.ShowError("ETSU", err.Error())
		return 1
	}

	var when time.Time
	if action == "date" {
		when, ok = ui.PromptDate()
		if !ok {
			return 0
		}
	}

	win, err := ui.NewWindow(action, file)
	if err != nil {
		ui.ShowError("ETSU", err.Error())
		return 1
	}

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()
	win.SetCancel(cancel)

	rep := &ui.Reporter{
		AppVersion:  app.Version,
		ExifVersion: app.ExifVersion,
		Sink:        win.Append,
	}
	rep.Header(action, file)
	win.Show()

	go func() {
		var opErr error
		switch action {
		case "read":
			opErr = ops.Read(ctx, file, rep)
		case "clean":
			opErr = ops.Clean(ctx, file, rep)
		case "date":
			opErr = ops.Date(ctx, file, when, rep)
		}

		switch {
		case opErr == nil:
		case errors.Is(opErr, context.Canceled):
			win.Append("Cancelled.")
		default:
			win.Append("Failed: " + opErr.Error())
		}

		win.Finish(opErr, app.WriteLog(action, rep.Lines))
	}()

	return win.Run()
}

func parseArgs(args []string) (action, file string, ok bool) {
	if len(args) != 2 {
		return "", "", false
	}

	action = strings.ToLower(args[0])
	switch action {
	case "read", "clean", "date":
	default:
		return "", "", false
	}

	file = args[1]
	if file == "" {
		return "", "", false
	}
	return action, file, true
}
