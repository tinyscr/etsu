package ui

import (
	"context"
	"errors"
	"fmt"
	"path/filepath"
	"strings"
	"time"

	"github.com/lxn/walk"
	. "github.com/lxn/walk/declarative"
	"github.com/lxn/win"

	"github.com/tinyscr/etsu/internal/app"
	"github.com/tinyscr/etsu/internal/ops"
)

const (
	windowWidth     = 620
	expandedHeight  = 440
	collapsedHeight = 190
	readWidth       = 760
	readHeight      = 560
	readMinWidth    = 600
	readMinHeight   = 400
	maxLogLines     = 5000
	autoCloseDelay  = 1500 * time.Millisecond
	// rsrc puts the manifest at resource ID 1 and the icon group at ID 2.
	iconResourceID = 2
)

// Task is the transient window that runs one Explorer action.
type Task struct {
	action string
	file   string
	when   time.Time

	mw       *walk.MainWindow
	status   *walk.Label
	progress *walk.ProgressBar
	toggle   *walk.PushButton
	log      *walk.TextEdit
	table    *walk.TableView
	model    *metadataModel
	primary  *walk.PushButton
	copy     *walk.PushButton

	ctx    context.Context
	cancel context.CancelFunc

	allLines []string
	shown    []string

	running   bool
	succeeded bool
	expanded  bool
	closing   bool
	autoClose *time.Timer
}

func NewTask(action, file string, when time.Time) (*Task, error) {
	t := &Task{
		action:   action,
		file:     file,
		when:     when,
		expanded: action == "read",
	}
	t.ctx, t.cancel = context.WithCancel(context.Background())
	if action == "read" {
		t.model = &metadataModel{}
	}

	width, height := windowWidth, collapsedHeight
	minWidth, minHeight := 480, collapsedHeight
	var children []Widget

	if action == "read" {
		width, height = readWidth, readHeight
		minWidth, minHeight = readMinWidth, readMinHeight
		children = t.readChildren()
	} else {
		toggleText := "Show log ▼"
		if t.expanded {
			height = expandedHeight
			toggleText = "Hide log ▲"
		}
		children = t.taskChildren(toggleText)
	}

	err := MainWindow{
		AssignTo: &t.mw,
		Title:    "ETSU - " + actionTitle(action) + " - " + filepath.Base(file),
		Size:     Size{Width: width, Height: height},
		MinSize:  Size{Width: minWidth, Height: minHeight},
		Font:     Font{Family: "Segoe UI", PointSize: 9},
		Layout:   VBox{Margins: Margins{Left: 10, Top: 10, Right: 10, Bottom: 10}, Spacing: 6},
		Children: children,
	}.Create()
	if err != nil {
		return nil, err
	}

	ApplyTheme(t.mw)

	if icon, err := walk.NewIconFromResourceId(iconResourceID); err == nil {
		t.mw.SetIcon(icon)
	}
	center(t.mw)
	t.mw.Closing().Attach(t.onClosing)
	t.mw.Starting().Attach(func() { t.start() })

	return t, nil
}

func (t *Task) readChildren() []Widget {
	return []Widget{
		Label{AssignTo: &t.status, Text: "Reading metadata..."},
		TableView{
			AssignTo:            &t.table,
			Model:               t.model,
			AlternatingRowBG:    !currentTheme().dark,
			LastColumnStretched: true,
			StretchFactor:       1,
			Columns: []TableViewColumn{
				{Title: "Tag", Width: 240},
				{Title: "Value", Width: 460},
			},
		},
		Composite{
			Layout: HBox{MarginsZero: true, Spacing: 6},
			Children: []Widget{
				HSpacer{},
				PushButton{AssignTo: &t.copy, Text: "Copy", OnClicked: t.copyLog},
				PushButton{AssignTo: &t.primary, Text: "Cancel", OnClicked: t.primaryClicked},
			},
		},
	}
}

func (t *Task) taskChildren(toggleText string) []Widget {
	return []Widget{
		Label{AssignTo: &t.status, Text: "Starting..."},
		ProgressBar{AssignTo: &t.progress, MaxValue: 100},
		PushButton{AssignTo: &t.toggle, Text: toggleText, OnClicked: t.toggleLog},
		TextEdit{
			AssignTo: &t.log,
			ReadOnly: true,
			VScroll:  true,
			Visible:  t.expanded,
			Font:     Font{Family: "Consolas", PointSize: 9},
		},
		Composite{
			Layout: HBox{MarginsZero: true, Spacing: 6},
			Children: []Widget{
				HSpacer{},
				PushButton{AssignTo: &t.copy, Text: "Copy log", OnClicked: t.copyLog},
				PushButton{AssignTo: &t.primary, Text: "Cancel", OnClicked: t.primaryClicked},
			},
		},
	}
}

func (t *Task) Run() int {
	return t.mw.Run()
}

func (t *Task) start() {
	t.running = true
	t.primary.SetText("Cancel")
	go func() {
		err := t.execute()
		t.mw.Synchronize(func() { t.finish(err) })
	}()
}

func (t *Task) execute() error {
	switch t.action {
	case "read":
		return ops.Read(t.ctx, t.file, t)
	case "clean":
		return ops.Clean(t.ctx, t.file, t)
	case "date":
		return ops.Date(t.ctx, t.file, t.when, t)
	default:
		return fmt.Errorf("unknown action %q", t.action)
	}
}

func (t *Task) Progress(percent int, label string) {
	t.mw.Synchronize(func() {
		if t.progress != nil {
			t.progress.SetValue(percent)
		}
		t.status.SetText(label)
	})
}

func (t *Task) Log(line string) {
	t.mw.Synchronize(func() {
		if t.action == "read" {
			t.allLines = append(t.allLines, line)
			if tag, value, ok := parseMetadataLine(line); ok && t.model != nil {
				t.model.Add(tag, value)
			}
			return
		}
		t.appendLog(line)
	})
}

func (t *Task) appendLog(line string) {
	t.allLines = append(t.allLines, line)
	t.shown = append(t.shown, line)

	if len(t.shown) > maxLogLines {
		t.shown = t.shown[len(t.shown)-maxLogLines:]
		t.log.SetText(strings.Join(t.shown, "\r\n"))
	} else {
		t.log.AppendText(line + "\r\n")
	}
	t.scrollToEnd()
}

func (t *Task) finish(err error) {
	t.running = false
	t.cancel()
	t.primary.SetText("Close")

	outcome := "SUCCESS"
	switch {
	case err == nil:
		t.succeeded = true
		if t.progress != nil {
			t.progress.SetValue(100)
		}
		if t.action == "read" {
			t.status.SetText(fmt.Sprintf("Done. %d fields.", t.model.RowCount()))
		} else {
			t.status.SetText("Done.")
		}
	case errors.Is(err, context.Canceled):
		outcome = "CANCELLED"
		if t.action == "read" {
			t.status.SetText("Cancelled.")
		} else {
			t.appendLog("[CANCELLED] Operation cancelled.")
			t.status.SetText("Cancelled.")
			t.setExpanded(true)
		}
	default:
		outcome = "FAILED"
		if len(t.allLines) == 0 {
			if t.action == "read" {
				t.allLines = append(t.allLines, "[ABORT] "+err.Error())
			} else {
				t.appendLog("[ABORT] " + err.Error())
			}
		}
		if t.action == "read" {
			t.status.SetText("Failed: " + err.Error())
		} else {
			t.status.SetText("Failed. See log.")
			t.setExpanded(true)
		}
	}

	app.WriteLog(t.action, outcome, t.allLines)

	if t.closing {
		t.mw.Close()
		return
	}
	if err == nil && t.action != "read" && !t.expanded {
		t.scheduleAutoClose()
	}
}

func (t *Task) toggleLog() {
	t.setExpanded(!t.expanded)
}

func (t *Task) setExpanded(expanded bool) {
	if t.log == nil || t.toggle == nil || t.expanded == expanded {
		return
	}
	t.expanded = expanded
	t.stopAutoClose()

	if expanded {
		t.log.SetVisible(true)
		t.toggle.SetText("Hide log ▲")
		t.setHeight(expandedHeight)
		t.scrollToEnd()
		return
	}

	t.log.SetVisible(false)
	t.toggle.SetText("Show log ▼")
	t.setHeight(collapsedHeight)
	if t.succeeded && !t.running && !t.closing {
		t.scheduleAutoClose()
	}
}

func (t *Task) setHeight(height int) {
	b := t.mw.Bounds()
	b.Height = height
	t.mw.SetBounds(b)
}

func (t *Task) scrollToEnd() {
	n := t.log.TextLength()
	t.log.SetTextSelection(n, n)
	t.log.SendMessage(win.EM_SCROLLCARET, 0, 0)
}

func (t *Task) copyLog() {
	t.stopAutoClose()
	if t.action == "read" {
		if t.model != nil {
			walk.Clipboard().SetText(t.model.Text())
		}
		return
	}
	walk.Clipboard().SetText(strings.Join(t.allLines, "\r\n"))
}

func (t *Task) primaryClicked() {
	if t.running {
		t.cancel()
		t.status.SetText("Cancelling...")
		return
	}
	t.mw.Close()
}

func (t *Task) onClosing(canceled *bool, reason walk.CloseReason) {
	t.stopAutoClose()
	if !t.running {
		return
	}

	*canceled = true
	if t.closing {
		return
	}
	t.closing = true
	t.cancel()
	t.status.SetText("Cancelling...")
}

func (t *Task) scheduleAutoClose() {
	t.stopAutoClose()
	t.autoClose = time.AfterFunc(autoCloseDelay, func() {
		t.mw.Synchronize(func() {
			if !t.expanded && !t.running && !t.closing {
				t.mw.Close()
			}
		})
	})
}

func (t *Task) stopAutoClose() {
	if t.autoClose != nil {
		t.autoClose.Stop()
		t.autoClose = nil
	}
}

func actionTitle(action string) string {
	switch action {
	case "read":
		return "Read metadata"
	case "clean":
		return "Clean metadata"
	case "date":
		return "Set date"
	default:
		return action
	}
}

func center(f walk.Form) {
	b := f.BoundsPixels()
	screenW := int(win.GetSystemMetrics(win.SM_CXSCREEN))
	screenH := int(win.GetSystemMetrics(win.SM_CYSCREEN))
	b.X = (screenW - b.Width) / 2
	b.Y = (screenH - b.Height) / 2
	f.SetBoundsPixels(b)
}
