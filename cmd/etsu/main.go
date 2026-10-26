package main

import (
	"fmt"
	"os"
	"strings"

	"github.com/tinyscr/etsu/internal/app"
	"github.com/tinyscr/etsu/internal/ops"
)

func main() {
	app.Init()
	os.Exit(run(os.Args[1:]))
}

func run(args []string) int {
	cmd := ""
	if len(args) > 0 {
		cmd = strings.ToLower(args[0])
	}

	switch cmd {
	case "read":
		if !app.FindExifTool() {
			return 1
		}
		return ops.Read(args[1:])
	case "clean":
		if !app.FindExifTool() {
			return 1
		}
		return ops.Clean(args[1:])
	case "date":
		if !app.FindExifTool() {
			return 1
		}
		return ops.Date(args[1:])
	default:
		fmt.Println("Usage: etsu read|clean|date [FILE...]")
		return 1
	}
}
