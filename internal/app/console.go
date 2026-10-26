package app

import (
	"os"

	"golang.org/x/sys/windows"
)

var vtEnabled bool

func initConsole() {
	_ = windows.SetConsoleOutputCP(65001)

	h := windows.Handle(os.Stdout.Fd())
	var mode uint32
	if err := windows.GetConsoleMode(h, &mode); err == nil {
		if err := windows.SetConsoleMode(h, mode|windows.ENABLE_VIRTUAL_TERMINAL_PROCESSING); err == nil {
			vtEnabled = true
		}
	}
}

func colorCode(c Color) string {
	switch c {
	case DarkGray:
		return "\x1b[90m"
	case Red:
		return "\x1b[31m"
	case Green:
		return "\x1b[32m"
	default:
		return "\x1b[37m"
	}
}
