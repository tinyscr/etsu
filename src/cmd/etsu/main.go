package main

import (
	"context"
	"errors"
	"os"
	"os/signal"
	"strings"

	"github.com/tinyscr/etsu/internal/app"
	"github.com/tinyscr/etsu/internal/ops"
	"github.com/tinyscr/etsu/internal/ui"
)

func main() {
	os.Exit(run(os.Args[1:]))
}

func run(args []string) int {
	ui.Init()

	if err := app.Init(); err != nil {
		ui.WriteLine(err.Error(), ui.Red)
		ui.WaitExit()
		return 1
	}

	action, file, ok := parseArgs(args)
	if !ok {
		ui.WriteLine("Usage: etsu read|clean|date <file>", ui.DarkGray)
		ui.WriteLine("ETSU normally runs from the Explorer context menu.", ui.DarkGray)
		ui.WaitExit()
		return 2
	}

	if err := app.FindExifTool(); err != nil {
		ui.WriteLine(err.Error(), ui.Red)
		ui.WaitExit()
		return 1
	}

	rep := &ui.Reporter{AppVersion: app.Version, ExifVersion: app.ExifVersion}
	rep.Header(action, file)

	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt)
	defer stop()

	var err error

	switch action {
	case "read":
		err = ops.Read(ctx, file, rep)
	case "clean":
		err = ops.Clean(ctx, file, rep)
	case "date":
		when, ok := ui.PromptDate()
		if !ok {
			ui.WaitExit()
			return 0
		}
		err = ops.Date(ctx, file, when, rep)
	}

	app.WriteLog(action, rep.Lines)

	switch {
	case err == nil:
		ui.WaitExit()
		return 0
	case errors.Is(err, context.Canceled):
		ui.WriteLine("Cancelled.", ui.Yellow)
		ui.WaitExit()
		return 1
	default:
		ui.WriteLine("Failed: "+err.Error(), ui.Red)
		ui.WaitExit()
		return 1
	}
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
