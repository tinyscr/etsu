package ui

import (
	"regexp"
	"strconv"
	"strings"
	"unicode/utf8"
)

const ruleWidth = 54

var (
	rule     = strings.Repeat("━", ruleWidth)
	stepLine = regexp.MustCompile(`^\[(\d+)/(\d+)\] `)
)

// Reporter formats operation output and forwards each line to Sink.
type Reporter struct {
	Lines       []string
	AppVersion  string
	ExifVersion string
	Sink        func(string)
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
	if r.Sink != nil {
		r.Sink(line)
	}
}

func (r *Reporter) blank(n int) {
	for range n {
		r.write("")
	}
}
