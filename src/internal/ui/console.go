package ui

import (
	"bufio"
	"fmt"
	"os"
	"regexp"
	"strconv"
	"strings"
	"time"
	"unicode/utf8"

	"golang.org/x/sys/windows"
)

var (
	vtEnabled bool
	stdin     = bufio.NewReader(os.Stdin)
	stepLine  = regexp.MustCompile(`^\[(\d+)/(\d+)\] `)
)

const ruleWidth = 54

var rule = strings.Repeat("━", ruleWidth)

func Init() {
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
	Yellow
)

func colorCode(c Color) string {
	switch c {
	case DarkGray:
		return "\x1b[90m"
	case Red:
		return "\x1b[31m"
	case Yellow:
		return "\x1b[33m"
	default:
		return "\x1b[37m"
	}
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

func readLine() string {
	line, _ := stdin.ReadString('\n')
	return strings.TrimSpace(line)
}

func PromptDate() (time.Time, bool) {
	fmt.Print("  Enter date (DD-MM-YYYY HH:MM:SS): ")
	line := readLine()
	if line == "" {
		WriteLine("No date entered.", DarkGray)
		return time.Time{}, false
	}

	dt, err := time.ParseInLocation("02-01-2006 15:04:05", line, time.Local)
	if err != nil {
		WriteLine("[ABORT] Invalid date. Expected format: DD-MM-YYYY HH:MM:SS", Red)
		return time.Time{}, false
	}
	return dt, true
}

func WaitExit() {
	fmt.Println()
	fmt.Println()
	WriteLine("Press enter to exit.", DarkGray)
	_, _ = stdin.ReadString('\n')
}

// Reporter streams the operation log to the console and keeps the full run
// for the optional log file.
type Reporter struct {
	Lines       []string
	AppVersion  string
	ExifVersion string
}

func (r *Reporter) Header(operation, file string) {
	lines := []string{
		"┏┓  ┏┳┓  ┏┓  ┳┳",
		"┣    ┃   ┗┓  ┃┃",
		"┗┛   ┻   ┗┛  ┗┛",
		rule,
		" App: " + r.AppVersion + " | Exiftool: " + r.ExifVersion + " | Operation: " + operation,
		"",
		" File(s): " + file,
		rule,
		"",
	}
	for _, line := range lines {
		r.write(line)
	}
}

func (r *Reporter) Progress(int, string) {}

func (r *Reporter) Log(line string) {
	m := stepLine.FindStringSubmatch(line)
	if m == nil {
		r.write(line)
		return
	}

	n, _ := strconv.Atoi(m[1])
	if n > 1 {
		r.blank(2)
	}
	r.write(line)

	width := ruleWidth
	if n > 1 {
		width = utf8.RuneCountInString(line) + 2
	}
	r.write(strings.Repeat("━", width))
}

func (r *Reporter) write(line string) {
	r.Lines = append(r.Lines, line)
	fmt.Println(line)
}

func (r *Reporter) blank(n int) {
	for range n {
		r.write("")
	}
}
