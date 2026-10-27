package ops

import (
	"context"
	"fmt"
	"os"
	"path/filepath"

	"github.com/tinyscr/etsu/internal/app"
)

func Read(ctx context.Context, file string, rep Reporter) error {
	if _, err := os.Stat(file); err != nil {
		rep.Log("[ABORT] File not found: " + file)
		return fmt.Errorf("File not found: %s", file)
	}

	rep.Log("File: " + file)
	rep.Progress(20, "Reading metadata")

	out, err := runExif(ctx, file)
	for _, line := range app.SplitLines(out) {
		rep.Log(line)
	}
	if err != nil {
		if ctx.Err() != nil {
			return ctx.Err()
		}
		rep.Log("[ABORT] ExifTool failed on " + filepath.Base(file))
		return err
	}

	rep.Progress(100, "Done")
	return nil
}
