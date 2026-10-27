package ops

import (
	"context"
	"fmt"
	"os"
	"path/filepath"
	"regexp"
	"strings"

	"github.com/tinyscr/etsu/internal/app"
)

var deletingTag = regexp.MustCompile(`Deleting\s+(.+)$`)

func Clean(ctx context.Context, file string, rep Reporter) error {
	if _, err := os.Stat(file); err != nil {
		rep.Log("[ABORT] File not found: " + file)
		return fmt.Errorf("File not found: %s", file)
	}

	rep.Log("ExifTool: " + app.ExifVersion + " (" + app.ExifPath + ")")
	rep.Log("File: " + file)

	err := Process(ctx, file, rep, "Cleaning metadata", func(temp, orig string) error {
		out, runErr := runExif(ctx, "-all=", "-overwrite_original", "-P", "-v", temp)
		if runErr != nil {
			if ctx.Err() != nil {
				return ctx.Err()
			}
			rep.Log("[ABORT] ExifTool failed on " + filepath.Base(orig))
			logExifOutput(rep, out)
			return runErr
		}

		var deleted []string
		for _, line := range app.SplitLines(out) {
			if m := deletingTag.FindStringSubmatch(strings.TrimSpace(line)); m != nil {
				deleted = append(deleted, m[1])
			}
		}

		rep.Log("  " + orig)
		if len(deleted) == 0 {
			rep.Log("    (no metadata found)")
		} else {
			for _, tag := range deleted {
				rep.Log("    - " + tag)
			}
		}
		return nil
	})
	if err != nil {
		return err
	}

	rep.Log("")
	rep.Log("All done. " + filepath.Base(file) + " cleaned in place.")
	return nil
}
