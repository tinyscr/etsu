package ui

import (
	"runtime"
	"strings"
	"sync"
	"time"
	"unsafe"

	"golang.org/x/sys/windows"
)

type dateDialog struct {
	hwnd      windows.Handle
	label     windows.Handle
	edit      windows.Handle
	okBtn     windows.Handle
	cancelBtn windows.Handle
	font      windows.Handle
	uiFont    windows.Handle
	dpi       uint32
	value     time.Time
	accepted  bool
}

var (
	dateDialogs     sync.Map
	dateEditOldProc uintptr
)

func PromptDate() (time.Time, bool) {
	d, err := newDateDialog()
	if err != nil {
		ShowError("ETSU", err.Error())
		return time.Time{}, false
	}
	return d.run()
}

func newDateDialog() (*dateDialog, error) {
	if err := initUI(); err != nil {
		return nil, err
	}

	dpi := systemDPI()
	d := &dateDialog{dpi: dpi}
	d.font = createFont("Consolas", 12, dpi)
	d.uiFont = createFont("Segoe UI", 10, dpi)
	if d.font == 0 || d.uiFont == 0 {
		deleteObject(d.font)
		deleteObject(d.uiFont)
		return nil, errFont
	}

	titlePtr, _ := windows.UTF16PtrFromString("ETSU — Set date")
	hwnd, _, callErr := procCreateWindowExW.Call(
		uintptr(wsExDialogModalFrame|wsExControlParent),
		uintptr(unsafe.Pointer(dateClassName)),
		uintptr(unsafe.Pointer(titlePtr)),
		uintptr(wsPopup|wsCaption|wsSysMenu|wsClipChildren),
		uintptr(cwUseDefault),
		uintptr(cwUseDefault),
		uintptr(scale(480, dpi)),
		uintptr(scale(220, dpi)),
		0, 0, hInstance, 0,
	)
	runtime.KeepAlive(titlePtr)
	if hwnd == 0 {
		deleteObject(d.font)
		deleteObject(d.uiFont)
		return nil, callError(callErr, "CreateWindowExW failed")
	}

	d.hwnd = windows.Handle(hwnd)
	dateDialogs.Store(hwnd, d)

	if err := d.createControls(); err != nil {
		dateDialogs.Delete(hwnd)
		destroyWindow(hwnd)
		deleteObject(d.font)
		deleteObject(d.uiFont)
		return nil, err
	}

	d.layout()
	centerWindow(hwnd)
	return d, nil
}

func (d *dateDialog) createControls() error {
	staticClass, _ := windows.UTF16PtrFromString("STATIC")
	editClass, _ := windows.UTF16PtrFromString("EDIT")
	buttonClass, _ := windows.UTF16PtrFromString("BUTTON")
	labelText, _ := windows.UTF16PtrFromString("Enter date (DD-MM-YYYY HH:MM:SS)")
	okText, _ := windows.UTF16PtrFromString("OK")
	cancelText, _ := windows.UTF16PtrFromString("Cancel")

	label, _, callErr := procCreateWindowExW.Call(
		0,
		uintptr(unsafe.Pointer(staticClass)),
		uintptr(unsafe.Pointer(labelText)),
		uintptr(wsChild|wsVisible),
		0, 0, 0, 0,
		uintptr(d.hwnd), 0, hInstance, 0,
	)
	if label == 0 {
		return callError(callErr, "could not create date label")
	}
	d.label = windows.Handle(label)

	edit, _, callErr := procCreateWindowExW.Call(
		uintptr(wsExClientEdge),
		uintptr(unsafe.Pointer(editClass)),
		0,
		uintptr(wsChild|wsVisible|wsTabStop|esLeft|esAutoHScroll),
		0, 0, 0, 0,
		uintptr(d.hwnd), 0, hInstance, 0,
	)
	if edit == 0 {
		return callError(callErr, "could not create date field")
	}
	d.edit = windows.Handle(edit)

	ok, _, callErr := procCreateWindowExW.Call(
		0,
		uintptr(unsafe.Pointer(buttonClass)),
		uintptr(unsafe.Pointer(okText)),
		uintptr(wsChild|wsVisible|wsTabStop|bsDefPushButton),
		0, 0, 0, 0,
		uintptr(d.hwnd), idOK, hInstance, 0,
	)
	if ok == 0 {
		return callError(callErr, "could not create date OK button")
	}
	d.okBtn = windows.Handle(ok)

	cancel, _, callErr := procCreateWindowExW.Call(
		0,
		uintptr(unsafe.Pointer(buttonClass)),
		uintptr(unsafe.Pointer(cancelText)),
		uintptr(wsChild|wsVisible|wsTabStop|bsPushButton),
		0, 0, 0, 0,
		uintptr(d.hwnd), idCancel, hInstance, 0,
	)
	if cancel == 0 {
		return callError(callErr, "could not create date Cancel button")
	}
	d.cancelBtn = windows.Handle(cancel)
	runtime.KeepAlive(staticClass)
	runtime.KeepAlive(editClass)
	runtime.KeepAlive(buttonClass)
	runtime.KeepAlive(labelText)
	runtime.KeepAlive(okText)
	runtime.KeepAlive(cancelText)

	sendMessage(label, wmSetFont, uintptr(d.uiFont), 1)
	sendMessage(edit, wmSetFont, uintptr(d.font), 1)
	sendMessage(ok, wmSetFont, uintptr(d.uiFont), 1)
	sendMessage(cancel, wmSetFont, uintptr(d.uiFont), 1)

	dateEditOldProc, _, _ = procSetWindowLongPtrW.Call(edit, gwlpWndProc, dateEditSubclass)
	return nil
}

func (d *dateDialog) layout() {
	if d.hwnd == 0 || d.edit == 0 {
		return
	}

	r := clientRect(uintptr(d.hwnd))
	margin := scale(16, d.dpi)
	labelHeight := scale(20, d.dpi)
	editHeight := scale(30, d.dpi)
	buttonWidth := scale(88, d.dpi)
	buttonHeight := scale(32, d.dpi)
	gap := scale(10, d.dpi)

	labelY := margin
	editY := labelY + labelHeight + scale(6, d.dpi)
	moveWindow(uintptr(d.label), margin, labelY, r.Right-2*margin, labelHeight, true)
	moveWindow(uintptr(d.edit), margin, editY, r.Right-2*margin, editHeight, true)

	buttonY := r.Bottom - margin - buttonHeight
	cancelX := r.Right - margin - buttonWidth
	okX := cancelX - gap - buttonWidth
	moveWindow(uintptr(d.okBtn), okX, buttonY, buttonWidth, buttonHeight, true)
	moveWindow(uintptr(d.cancelBtn), cancelX, buttonY, buttonWidth, buttonHeight, true)
}

func (d *dateDialog) run() (time.Time, bool) {
	procShowWindow.Call(uintptr(d.hwnd), swShow)
	procUpdateWindow.Call(uintptr(d.hwnd))
	setFocus(uintptr(d.edit))

	var m msg
	for {
		r, _, _ := procGetMessageW.Call(uintptr(unsafe.Pointer(&m)), 0, 0, 0)
		if int32(r) <= 0 {
			break
		}
		if d.hwnd != 0 {
			handled, _, _ := procIsDialogMessageW.Call(uintptr(d.hwnd), uintptr(unsafe.Pointer(&m)))
			if handled != 0 {
				continue
			}
		}
		procTranslateMessage.Call(uintptr(unsafe.Pointer(&m)))
		procDispatchMessageW.Call(uintptr(unsafe.Pointer(&m)))
	}
	return d.value, d.accepted
}

func (d *dateDialog) accept() {
	text := strings.TrimSpace(getWindowText(uintptr(d.edit)))
	if text == "" {
		showMessage(uintptr(d.hwnd), "ETSU", "Enter a date.", mbOK|mbIconWarning)
		return
	}

	value, err := time.ParseInLocation("02-01-2006 15:04:05", text, time.Local)
	if err != nil {
		showMessage(uintptr(d.hwnd), "ETSU", "Invalid date.\n\nExpected format: DD-MM-YYYY HH:MM:SS", mbOK|mbIconWarning)
		return
	}

	d.value = value
	d.accepted = true
	destroyWindow(uintptr(d.hwnd))
}

func (d *dateDialog) controlColor(hdc uintptr) uintptr {
	procSetBkMode.Call(hdc, opaque)
	procSetBkColor.Call(hdc, bgColor)
	procSetTextColor.Call(hdc, fgColor)
	return uiBrush
}

func dateWndProc(hwnd, message, wParam, lParam uintptr) uintptr {
	value, ok := dateDialogs.Load(hwnd)
	if !ok {
		return defWindowProc(hwnd, message, wParam, lParam)
	}
	d := value.(*dateDialog)

	switch message {
	case wmSize:
		d.layout()
		return 0
	case wmCommand:
		switch uint16(wParam & 0xffff) {
		case idOK:
			d.accept()
		case idCancel:
			destroyWindow(uintptr(d.hwnd))
		}
		return 0
	case wmClose:
		destroyWindow(uintptr(d.hwnd))
		return 0
	case wmCtlColorStatic, wmCtlColorEdit:
		return d.controlColor(wParam)
	case wmDestroy:
		dateDialogs.Delete(hwnd)
		d.hwnd = 0
		deleteObject(d.font)
		deleteObject(d.uiFont)
		d.font = 0
		d.uiFont = 0
		procPostQuitMessage.Call(0)
		return 0
	}

	return defWindowProc(hwnd, message, wParam, lParam)
}

func dateEditSubclassProc(hwnd, message, wParam, lParam uintptr) uintptr {
	if message == wmKeyDown {
		parent, _, _ := procGetParent.Call(hwnd)
		if parent != 0 {
			switch wParam {
			case vkReturn:
				postMessage(parent, wmCommand, idOK, 0)
				return 0
			case vkEscape:
				postMessage(parent, wmCommand, idCancel, 0)
				return 0
			}
		}
	}

	if dateEditOldProc == 0 {
		return 0
	}
	return callWindowProc(dateEditOldProc, hwnd, message, wParam, lParam)
}
