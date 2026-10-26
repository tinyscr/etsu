package ops

import (
	"bufio"
	"fmt"
	"os"
	"path/filepath"
	"strconv"
	"strings"
	"time"

	"github.com/tinyscr/etsu/internal/app"
)

func Date(paths []string) int {
	app.WriteHeader("Date")
	logLines := []string{
		"ExifTool path: " + app.ExifPath,
		"ExifTool version: " + app.ExifVersion,
	}

	if len(paths) == 0 {
		app.WriteLine("date takes at least one file path.", app.Red)
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

	fmt.Print("  Enter date (DD-MM-YYYY HH:MM:SS): ")
	input, _ := bufio.NewReader(os.Stdin).ReadString('\n')
	input = strings.TrimSpace(input)
	if input == "" {
		app.WriteLine("No date entered.", app.DarkGray)
		return 0
	}

	dt, err := time.ParseInLocation("02-01-2006 15:04:05", input, time.Local)
	if err != nil {
		app.WriteLine("[ABORT] Invalid date. Expected format: DD-MM-YYYY HH:MM:SS", app.Red)
		logLines = append(logLines, "[ABORT] Invalid date input: "+input)
		return 1
	}
	exifDate := dt.Format("2006:01:02 15:04:05")

	logLines = append(logLines, "Target date: "+input)
	fmt.Println()
	app.WriteStep(0, 5, "Target date: "+input, "")

	err = Process(paths, &logLines, "Setting date on files...", func(temp, orig string) error {
		out, runErr := runExif(
			"-AllDates="+exifDate,
			"-FileModifyDate="+exifDate,
			"-FileCreateDate="+exifDate,
			"-overwrite_original",
			"-P",
			temp,
		)
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

		logLines = append(logLines, "  Set OK: "+filepath.Base(orig))
		return nil
	})
	if err != nil {
		return 1
	}

	// Windows file-system tunneling can restore the previous creation time when
	// the swapped file reuses the original name, so set it once more explicitly.
	for _, file := range paths {
		if err := app.SetCreationTime(file, dt); err != nil {
			app.WriteLine("Warning: could not set creation time on "+filepath.Base(file)+": "+err.Error(), app.DarkGray)
		}
	}

	logLines = append(logLines, "", fmt.Sprintf("All done. %d file(s) date set in place.", len(paths)))
	app.WriteSep()
	app.WriteStep(5, 5, "Done!", " - "+strconv.Itoa(len(paths))+" file(s) date set in place.")
	app.WriteLog("date", "SUCCESS", logLines)
	return app.WaitExit()
}
