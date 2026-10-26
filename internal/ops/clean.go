package ops

import (
	"fmt"
	"os"
	"path/filepath"
	"regexp"
	"strconv"
	"strings"

	"github.com/tinyscr/etsu/internal/app"
)

var deletingTag = regexp.MustCompile(`Deleting\s+(.+)$`)

func Clean(paths []string) int {
	app.WriteHeader("Clean")
	logLines := []string{
		"ExifTool path: " + app.ExifPath,
		"ExifTool version: " + app.ExifVersion,
	}

	if len(paths) == 0 {
		app.WriteLine("clean takes at least one file path.", app.Red)
		return 2
	}
	for _, p := range paths {
		if _, err := os.Stat(p); err != nil {
			app.WriteLine("File not found: "+p, app.Red)
			return 2
		}
	}

	fmt.Println()
	app.WriteLine("Selected files:", app.DarkGray)
	fmt.Println()
	for _, f := range paths {
		fmt.Println("      " + filepath.Base(f))
	}
	logLines = append(logLines, "Files selected ("+strconv.Itoa(len(paths))+"):")
	for _, f := range paths {
		logLines = append(logLines, "  "+f)
	}

	app.WriteSep()
	err := Process(paths, &logLines, "Cleaning metadata...", func(temp, orig string) error {
		out, runErr := runExif("-all=", "-overwrite_original", "-P", "-v", temp)
		if runErr != nil {
			msg := "[ABORT] ExifTool failed on " + filepath.Base(orig)
			app.WriteLine(msg, app.Red)
			logLines = append(logLines, msg)
			for _, line := range app.SplitLines(out) {
				app.WriteLine("  "+line, app.DarkGray)
				logLines = append(logLines, "  "+line)
			}
			return runErr
		}

		var deleted []string
		for _, line := range app.SplitLines(out) {
			if m := deletingTag.FindStringSubmatch(strings.TrimSpace(line)); m != nil {
				deleted = append(deleted, m[1])
			}
		}

		logLines = append(logLines, "  "+orig)
		if len(deleted) == 0 {
			logLines = append(logLines, "    (no metadata found)")
		} else {
			for _, tag := range deleted {
				logLines = append(logLines, "    - "+tag)
			}
		}
		return nil
	})
	if err != nil {
		return 1
	}

	logLines = append(logLines, "", fmt.Sprintf("All done. %d file(s) cleaned in place.", len(paths)))
	app.WriteSep()
	app.WriteStep(5, 5, "Done!", " - "+strconv.Itoa(len(paths))+" file(s) cleaned in place.")
	app.WriteLog("clean", "SUCCESS", logLines)
	return app.WaitExit()
}
