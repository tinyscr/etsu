package ops

import (
	"fmt"
	"os"
	"path/filepath"

	"github.com/tinyscr/etsu/internal/app"
)

func Read(paths []string) int {
	app.WriteHeader("Read")

	if len(paths) != 1 {
		app.WriteLine("read takes exactly one file path.", app.Red)
		return 2
	}

	file := paths[0]
	if _, err := os.Stat(file); err != nil {
		app.WriteLine("File not found: "+file, app.Red)
		return 2
	}

	fmt.Println("  Selected: " + filepath.Base(file))
	app.WriteSep()

	output := app.RunTool(app.ExifPath, file)
	var logLines []string
	for _, line := range app.SplitLines(output) {
		fmt.Println("  " + line)
		logLines = append(logLines, line)
	}

	fmt.Println()
	app.WriteSep()
	app.WriteLog("read", "SUCCESS", logLines)
	return app.WaitExit()
}
