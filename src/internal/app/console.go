package app

import (
	"bufio"
	"fmt"
	"os"
	"strings"

	"golang.org/x/sys/windows"
)

var (
	vtEnabled bool
	stdin     = bufio.NewReader(os.Stdin)
)

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

type Color int

const (
	Gray Color = iota
	DarkGray
	Red
	Green
	Yellow
)

func colorCode(c Color) string {
	switch c {
	case DarkGray:
		return "\x1b[90m"
	case Red:
		return "\x1b[31m"
	case Green:
		return "\x1b[32m"
	case Yellow:
		return "\x1b[33m"
	default:
		return "\x1b[37m"
	}
}

func WriteHeader(mode string) {
	fmt.Println()
	fmt.Println("  ETSU  |  " + mode + "  |  ExifTool v" + ExifVersion)
	fmt.Println("  " + strings.Repeat("-", 56))
	fmt.Println()
}

func WriteLine(text string, color ...Color) {
	c := Gray
	if len(color) > 0 {
		c = color[0]
	}
	if vtEnabled {
		fmt.Print(colorCode(c))
	}
	fmt.Println("  " + text)
	if vtEnabled {
		fmt.Print("\x1b[0m")
	}
}

func WriteSep() {
	fmt.Println()
	WriteLine(strings.Repeat("-", 56), DarkGray)
	fmt.Println()
}

func WriteStep(num, total int, label, status string) {
	WriteLine(fmt.Sprintf("[%d/%d] - %s%s", num, total, label, status), DarkGray)
}

func ReadLine() string {
	line, _ := stdin.ReadString('\n')
	return strings.TrimSpace(line)
}

func WaitExit() {
	WriteLine("Press enter to exit.", DarkGray)
	_, _ = stdin.ReadString('\n')
}

// ConsoleReporter streams the operation log to the console and keeps the full
// run for the optional log file.
type ConsoleReporter struct {
	Lines []string
}

func (r *ConsoleReporter) Progress(int, string) {}

func (r *ConsoleReporter) Log(line string) {
	r.Lines = append(r.Lines, line)
	fmt.Println(line)
}
