package ui

import (
	"strings"
	"time"

	"github.com/lxn/walk"
	. "github.com/lxn/walk/declarative"
)

// AskDate shows the date dialog. It returns false when the user cancels.
func AskDate() (time.Time, bool) {
	var dlg *walk.Dialog
	var date *walk.DateEdit
	var clock *walk.LineEdit
	var okButton, cancelButton *walk.PushButton

	var result time.Time
	accepted := false

	err := Dialog{
		AssignTo:  &dlg,
		Title:     "ETSU - Set date",
		Size:      Size{Width: 320, Height: 220},
		FixedSize: true,
		Font:      Font{Family: "Segoe UI", PointSize: 9},
		Layout:    VBox{Margins: Margins{Left: 12, Top: 12, Right: 12, Bottom: 12}, Spacing: 8},
		Children: []Widget{
			Label{Text: "Date and time to write:"},
			DateEdit{AssignTo: &date},
			Label{Text: "Time (HH:MM:SS):"},
			LineEdit{AssignTo: &clock, Text: time.Now().Format("15:04:05")},
			Composite{
				Layout: HBox{MarginsZero: true, Spacing: 8},
				Children: []Widget{
					HSpacer{},
					PushButton{AssignTo: &cancelButton, Text: "Cancel", OnClicked: func() { dlg.Cancel() }},
					PushButton{AssignTo: &okButton, Text: "OK", OnClicked: func() {
						parsed, err := time.ParseInLocation("15:04:05", strings.TrimSpace(clock.Text()), time.Local)
						if err != nil {
							walk.MsgBox(dlg, "ETSU", "Enter the time as HH:MM:SS.", walk.MsgBoxIconWarning)
							return
						}
						d := date.Date()
						result = time.Date(d.Year(), d.Month(), d.Day(), parsed.Hour(), parsed.Minute(), parsed.Second(), 0, time.Local)
						accepted = true
						dlg.Accept()
					}},
				},
			},
		},
		CancelButton:  &cancelButton,
		DefaultButton: &okButton,
	}.Create(nil)
	if err != nil {
		return time.Time{}, false
	}

	if icon, err := walk.NewIconFromResourceId(iconResourceID); err == nil {
		dlg.SetIcon(icon)
	}
	date.SetDate(time.Now())
	center(dlg)
	dlg.Run()

	return result, accepted
}
