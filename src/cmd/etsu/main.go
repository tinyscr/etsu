package main

import (
	"os"
	"strings"
	"time"

	"github.com/lxn/walk"
	"github.com/tinyscr/etsu/internal/app"
	"github.com/tinyscr/etsu/internal/ui"
)

const usage = "ETSU runs from the Explorer context menu.\n\n" +
	"Right-click a file and choose ETSU, then Read metadata, Clean metadata, or Set date."

func main() {
	ui.InitTheme()
	app.Init()

	action, file, ok := parseArgs(os.Args[1:])
	if !ok {
		walk.MsgBox(nil, "ETSU", usage, walk.MsgBoxIconInformation)
		return
	}

	if err := app.FindExifTool(); err != nil {
		walk.MsgBox(nil, "ETSU", err.Error(), walk.MsgBoxIconError)
		return
	}

	var when time.Time
	if action == "date" {
		var accepted bool
		when, accepted = ui.AskDate()
		if !accepted {
			return
		}
	}

	task, err := ui.NewTask(action, file, when)
	if err != nil {
		walk.MsgBox(nil, "ETSU", err.Error(), walk.MsgBoxIconError)
		return
	}
	task.Run()
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
