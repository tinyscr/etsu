package main

import (
	"context"
	"errors"
	"fmt"
	"os"
	"os/signal"
	"strings"
	"time"

	"github.com/tinyscr/etsu/internal/app"
	"github.com/tinyscr/etsu/internal/ops"
)

func main() {
	os.Exit(run(os.Args[1:]))
}

func run(args []string) int {
	if err := app.Init(); err != nil {
		app.WriteLine(err.Error(), app.Red)
		app.WaitExit()
		return 1
	}

	action, file, ok := parseArgs(args)
	if !ok {
		app.WriteLine("Usage: etsu read|clean|date <file>", app.DarkGray)
		app.WriteLine("ETSU normally runs from the Explorer context menu.", app.DarkGray)
		app.WaitExit()
		return 2
	}

	if err := app.FindExifTool(); err != nil {
		app.WriteLine(err.Error(), app.Red)
		app.WaitExit()
		return 1
	}

	app.WriteHeader(actionTitle(action))

	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt)
	defer stop()

	rep := &app.ConsoleReporter{}
	var err error

	switch action {
	case "read":
		err = ops.Read(ctx, file, rep)
	case "clean":
		err = ops.Clean(ctx, file, rep)
	case "date":
		when, ok := promptDate()
		if !ok {
			app.WaitExit()
			return 0
		}
		err = ops.Date(ctx, file, when, rep)
	}

	outcome := "SUCCESS"
	if err != nil {
		if errors.Is(err, context.Canceled) {
			outcome = "CANCELLED"
		} else {
			outcome = "FAILED"
		}
	}
	app.WriteLog(action, outcome, rep.Lines)

	switch {
	case err == nil:
		app.WriteLine("Done.", app.Green)
		app.WaitExit()
		return 0
	case errors.Is(err, context.Canceled):
		app.WriteLine("Cancelled.", app.Yellow)
		app.WaitExit()
		return 1
	default:
		app.WriteLine("Failed: "+err.Error(), app.Red)
		app.WaitExit()
		return 1
	}
}

func promptDate() (time.Time, bool) {
	fmt.Print("  Enter date (DD-MM-YYYY HH:MM:SS): ")
	line := app.ReadLine()
	if line == "" {
		app.WriteLine("No date entered.", app.DarkGray)
		return time.Time{}, false
	}

	dt, err := time.ParseInLocation("02-01-2006 15:04:05", line, time.Local)
	if err != nil {
		app.WriteLine("[ABORT] Invalid date. Expected format: DD-MM-YYYY HH:MM:SS", app.Red)
		return time.Time{}, false
	}
	return dt, true
}

func actionTitle(action string) string {
	switch action {
	case "read":
		return "Read metadata"
	case "clean":
		return "Clean metadata"
	case "date":
		return "Set date"
	default:
		return action
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
