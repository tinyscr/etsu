package ops

import (
	"context"
	"fmt"
	"os"
	"path/filepath"
	"time"

	"github.com/tinyscr/etsu/internal/app"
)

func Date(ctx context.Context, file string, dt time.Time, rep Reporter) error {
	if _, err := os.Stat(file); err != nil {
		rep.Log("[ABORT] File not found: " + file)
		return fmt.Errorf("File not found: %s", file)
	}

	rep.Log("Target date: " + dt.Format("02-01-2006 15:04:05"))
	rep.Log("")

	exifDate := dt.Format("2006:01:02 15:04:05")
	err := Process(ctx, file, rep, "Setting date", func(temp, orig string) error {
		out, runErr := runExif(
			ctx,
			"-AllDates="+exifDate,
			"-FileModifyDate="+exifDate,
			"-FileCreateDate="+exifDate,
			"-overwrite_original",
			"-P",
			temp,
		)
		if runErr != nil {
			if ctx.Err() != nil {
				return ctx.Err()
			}
			rep.Log("[ABORT] ExifTool failed on " + filepath.Base(orig))
			logExifOutput(rep, out)
			return runErr
		}

		rep.Log("  Set OK: " + filepath.Base(orig))
		return nil
	})
	if err != nil {
		return err
	}

	// Windows file-system tunneling can restore the previous creation time when
	// the swapped file reuses the original name, so set it once more explicitly.
	if err := app.SetCreationTime(file, dt); err != nil {
		rep.Log("Warning: could not set creation time on " + filepath.Base(file) + ": " + err.Error())
	}

	rep.Log("All done. 1 file date set in place.")
	return nil
}
