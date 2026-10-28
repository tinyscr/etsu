package ui

import (
	"path/filepath"
	"runtime"
	"sync"
	"unsafe"

	"golang.org/x/sys/windows"
)

// Window is the native text window used for one ETSU operation.
type Window struct {
	hwnd     windows.Handle
	edit     windows.Handle
	openBtn  windows.Handle
	okBtn    windows.Handle
	font     windows.Handle
	uiFont   windows.Handle
	dpi      uint32
	fontSize int

	mu      sync.Mutex
	queue   []string
	done    bool
	closing bool
	logPath string
	result  int
	cancel  func()
}

func NewWindow(operation, file string) (*Window, error) {
	if err := initUI(); err != nil {
		return nil, err
	}

	dpi := systemDPI()
	w := &Window{dpi: dpi, fontSize: 12}
	w.font = createFont("Consolas", w.fontSize, dpi)
	w.uiFont = createFont("Segoe UI", 10, dpi)
	if w.font == 0 || w.uiFont == 0 {
		deleteObject(w.font)
		deleteObject(w.uiFont)
		return nil, errFont
	}

	title := "ETSU — " + operationTitle(operation)
	if base := filepath.Base(file); base != "" && len([]rune(base)) <= 48 {
		title += " — " + base
	}
	titlePtr, _ := windows.UTF16PtrFromString(title)

	width := scale(1040, dpi)
	height := scale(720, dpi)
	if screenWidth, _, _ := procGetSystemMetrics.Call(smCXScreen); screenWidth != 0 {
		if maxWidth := int32(screenWidth) - scale(48, dpi); width > maxWidth {
			width = maxWidth
		}
	}
	if screenHeight, _, _ := procGetSystemMetrics.Call(smCYScreen); screenHeight != 0 {
		if maxHeight := int32(screenHeight) - scale(48, dpi); height > maxHeight {
			height = maxHeight
		}
	}

	hwnd, _, callErr := procCreateWindowExW.Call(
		uintptr(wsExAppWindow|wsExControlParent),
		uintptr(unsafe.Pointer(mainClassName)),
		uintptr(unsafe.Pointer(titlePtr)),
		uintptr(wsOverlappedWindow|wsClipChildren),
		uintptr(cwUseDefault),
		uintptr(cwUseDefault),
		uintptr(width),
		uintptr(height),
		0, 0, hInstance, 0,
	)
	runtime.KeepAlive(titlePtr)
	if hwnd == 0 {
		deleteObject(w.font)
		deleteObject(w.uiFont)
		return nil, callError(callErr, "CreateWindowExW failed")
	}

	w.hwnd = windows.Handle(hwnd)
	mainWindows.Store(hwnd, w)

	if err := w.createControls(); err != nil {
		mainWindows.Delete(hwnd)
		destroyWindow(hwnd)
		deleteObject(w.font)
		deleteObject(w.uiFont)
		return nil, err
	}

	w.layout()
	centerWindow(hwnd)
	return w, nil
}

func (w *Window) createControls() error {
	editClass, _ := windows.UTF16PtrFromString("EDIT")
	buttonClass, _ := windows.UTF16PtrFromString("BUTTON")
	openText, _ := windows.UTF16PtrFromString("Open log")
	okText, _ := windows.UTF16PtrFromString("OK")

	edit, _, callErr := procCreateWindowExW.Call(
		uintptr(wsExClientEdge),
		uintptr(unsafe.Pointer(editClass)),
		0,
		uintptr(wsChild|wsVisible|wsTabStop|wsVScroll|wsHScroll|
			esMultiline|esReadOnly|esAutoVScroll|esAutoHScroll|esNoHideSel|esCenter),
		0, 0, 0, 0,
		uintptr(w.hwnd), idEdit, hInstance, 0,
	)
	if edit == 0 {
		return callError(callErr, "could not create text window")
	}
	w.edit = windows.Handle(edit)

	open, _, callErr := procCreateWindowExW.Call(
		0,
		uintptr(unsafe.Pointer(buttonClass)),
		uintptr(unsafe.Pointer(openText)),
		uintptr(wsChild|wsVisible|wsTabStop|bsPushButton),
		0, 0, 0, 0,
		uintptr(w.hwnd), idOpenLog, hInstance, 0,
	)
	if open == 0 {
		return callError(callErr, "could not create Open log button")
	}
	w.openBtn = windows.Handle(open)

	ok, _, callErr := procCreateWindowExW.Call(
		0,
		uintptr(unsafe.Pointer(buttonClass)),
		uintptr(unsafe.Pointer(okText)),
		uintptr(wsChild|wsVisible|wsTabStop|bsDefPushButton),
		0, 0, 0, 0,
		uintptr(w.hwnd), idOK, hInstance, 0,
	)
	if ok == 0 {
		return callError(callErr, "could not create OK button")
	}
	w.okBtn = windows.Handle(ok)
	runtime.KeepAlive(editClass)
	runtime.KeepAlive(buttonClass)
	runtime.KeepAlive(openText)
	runtime.KeepAlive(okText)

	sendMessage(edit, wmSetFont, uintptr(w.font), 1)
	sendMessage(open, wmSetFont, uintptr(w.uiFont), 1)
	sendMessage(ok, wmSetFont, uintptr(w.uiFont), 1)

	margin := uintptr(scale(12, w.dpi))
	sendMessage(edit, emSetMargins, ecLeftMargin|ecRightMargin, margin|margin<<16)
	sendMessage(edit, emSetLimitText, 0x7ffffffe, 0)

	enableWindow(open, false)
	enableWindow(ok, false)

	mainEditOldProc, _, _ = procSetWindowLongPtrW.Call(edit, gwlpWndProc, mainEditSubclass)
	return nil
}

func operationTitle(operation string) string {
	switch operation {
	case "read":
		return "Read metadata"
	case "clean":
		return "Clean metadata"
	case "date":
		return "Set date"
	default:
		return operation
	}
}

func (w *Window) Show() {
	procShowWindow.Call(uintptr(w.hwnd), swShow)
	procUpdateWindow.Call(uintptr(w.hwnd))
	setFocus(uintptr(w.edit))
	procHideCaret.Call(uintptr(w.edit))
}

func (w *Window) SetCancel(cancel func()) {
	w.mu.Lock()
	w.cancel = cancel
	w.mu.Unlock()
}

func (w *Window) Append(line string) {
	w.mu.Lock()
	w.queue = append(w.queue, line)
	hwnd := w.hwnd
	w.mu.Unlock()
	postMessage(uintptr(hwnd), wmAppend, 0, 0)
}

func (w *Window) Finish(err error, logPath string) {
	w.mu.Lock()
	w.done = true
	w.logPath = logPath
	if err != nil {
		w.result = 1
	}
	hwnd := w.hwnd
	w.mu.Unlock()
	postMessage(uintptr(hwnd), wmDone, 0, 0)
}

func (w *Window) Run() int {
	var m msg
	for {
		r, _, _ := procGetMessageW.Call(uintptr(unsafe.Pointer(&m)), 0, 0, 0)
		if int32(r) <= 0 {
			break
		}

		w.mu.Lock()
		hwnd := w.hwnd
		w.mu.Unlock()
		if hwnd != 0 {
			handled, _, _ := procIsDialogMessageW.Call(uintptr(hwnd), uintptr(unsafe.Pointer(&m)))
			if handled != 0 {
				continue
			}
		}
		procTranslateMessage.Call(uintptr(unsafe.Pointer(&m)))
		procDispatchMessageW.Call(uintptr(unsafe.Pointer(&m)))
	}

	w.mu.Lock()
	result := w.result
	w.mu.Unlock()
	return result
}

func (w *Window) layout() {
	if w.hwnd == 0 || w.edit == 0 {
		return
	}

	r := clientRect(uintptr(w.hwnd))
	margin := scale(16, w.dpi)
	buttonWidth := scale(112, w.dpi)
	buttonHeight := scale(36, w.dpi)
	gap := scale(10, w.dpi)

	editWidth := r.Right - 2*margin
	editHeight := r.Bottom - margin - buttonHeight - margin
	if editWidth < scale(100, w.dpi) {
		editWidth = scale(100, w.dpi)
	}
	if editHeight < scale(100, w.dpi) {
		editHeight = scale(100, w.dpi)
	}

	moveWindow(uintptr(w.edit), margin, margin, editWidth, editHeight, true)

	buttonY := r.Bottom - margin - buttonHeight
	okX := r.Right - margin - buttonWidth
	openX := okX - gap - buttonWidth
	moveWindow(uintptr(w.openBtn), openX, buttonY, buttonWidth, buttonHeight, true)
	moveWindow(uintptr(w.okBtn), okX, buttonY, buttonWidth, buttonHeight, true)
}

func (w *Window) minMax(lParam uintptr) {
	info := (*minMaxInfo)(unsafe.Pointer(lParam))
	info.MinTrackSize = point{
		X: scale(760, w.dpi),
		Y: scale(520, w.dpi),
	}
}

func (w *Window) dpiChanged(wParam, lParam uintptr) {
	dpi := uint32(wParam & 0xffff)
	if dpi != 0 {
		w.dpi = dpi
	}
	w.applyFonts()

	r := (*rect)(unsafe.Pointer(lParam))
	procSetWindowPos.Call(
		uintptr(w.hwnd),
		0,
		uintptr(r.Left),
		uintptr(r.Top),
		uintptr(r.Right-r.Left),
		uintptr(r.Bottom-r.Top),
		swpNoZOrder|swpNoActivate,
	)
	w.layout()
}

func (w *Window) applyFonts() {
	font := createFont("Consolas", w.fontSize, w.dpi)
	if font != 0 {
		sendMessage(uintptr(w.edit), wmSetFont, uintptr(font), 1)
		deleteObject(w.font)
		w.font = font
	}

	uiFont := createFont("Segoe UI", 10, w.dpi)
	if uiFont != 0 {
		sendMessage(uintptr(w.openBtn), wmSetFont, uintptr(uiFont), 1)
		sendMessage(uintptr(w.okBtn), wmSetFont, uintptr(uiFont), 1)
		deleteObject(w.uiFont)
		w.uiFont = uiFont
	}
}

func (w *Window) zoom(delta int) {
	next := w.fontSize + delta
	if next < 8 {
		next = 8
	}
	if next > 28 {
		next = 28
	}
	if next == w.fontSize {
		return
	}
	w.fontSize = next
	w.applyFonts()
}

func (w *Window) resetZoom() {
	if w.fontSize == 12 {
		return
	}
	w.fontSize = 12
	w.applyFonts()
}

func (w *Window) controlColor(hdc uintptr) uintptr {
	procSetBkMode.Call(hdc, opaque)
	procSetBkColor.Call(hdc, bgColor)
	procSetTextColor.Call(hdc, fgColor)
	return uiBrush
}

func (w *Window) flush() {
	w.mu.Lock()
	lines := w.queue
	w.queue = nil
	edit := w.edit
	w.mu.Unlock()
	if len(lines) == 0 || edit == 0 {
		return
	}

	sendMessage(uintptr(edit), wmSetRedraw, 0, 0)
	for _, line := range lines {
		appendText(edit, line)
	}
	sendMessage(uintptr(edit), wmSetRedraw, 1, 0)
	invalidateRect(uintptr(edit), true)
	sendMessage(uintptr(edit), emScrollCaret, 0, 0)
}

func appendText(edit windows.Handle, line string) {
	textPtr, err := windows.UTF16PtrFromString(line + "\r\n")
	if err != nil {
		return
	}
	length := sendMessage(uintptr(edit), wmGetTextLength, 0, 0)
	sendMessage(uintptr(edit), emSetSel, length, length)
	sendMessage(uintptr(edit), emReplaceSel, 0, uintptr(unsafe.Pointer(textPtr)))
	runtime.KeepAlive(textPtr)
}

func (w *Window) finishUI() {
	w.flush()

	w.mu.Lock()
	done := w.done
	closing := w.closing
	logPath := w.logPath
	w.mu.Unlock()
	if !done {
		return
	}

	if logPath != "" {
		enableWindow(uintptr(w.openBtn), true)
	}
	enableWindow(uintptr(w.okBtn), true)

	if closing {
		w.destroy()
		return
	}
	setFocus(uintptr(w.okBtn))
}

func (w *Window) close() {
	w.mu.Lock()
	done := w.done
	closing := w.closing
	cancel := w.cancel
	w.closing = true
	w.mu.Unlock()

	if done {
		w.destroy()
		return
	}
	if !closing {
		if cancel != nil {
			cancel()
		}
		w.Append("Cancelling...")
	}
}

func (w *Window) ok() {
	w.mu.Lock()
	done := w.done
	w.mu.Unlock()
	if done {
		w.destroy()
	}
}

func (w *Window) openLog() {
	w.mu.Lock()
	logPath := w.logPath
	w.mu.Unlock()
	if logPath != "" {
		openPath(logPath)
	}
}

func (w *Window) destroy() {
	w.mu.Lock()
	hwnd := w.hwnd
	w.mu.Unlock()
	destroyWindow(uintptr(hwnd))
}

func mainWndProc(hwnd, message, wParam, lParam uintptr) uintptr {
	value, ok := mainWindows.Load(hwnd)
	if !ok {
		return defWindowProc(hwnd, message, wParam, lParam)
	}
	w := value.(*Window)

	switch message {
	case wmSize:
		w.layout()
		return 0
	case wmCommand:
		switch uint16(wParam & 0xffff) {
		case idOpenLog:
			w.openLog()
		case idOK:
			w.ok()
		case idCancel:
			w.close()
		}
		return 0
	case wmClose:
		w.close()
		return 0
	case wmGetMinMaxInfo:
		w.minMax(lParam)
		return 0
	case wmDpiChanged:
		w.dpiChanged(wParam, lParam)
		return 0
	case wmAppend:
		w.flush()
		return 0
	case wmDone:
		w.finishUI()
		return 0
	case wmCtlColorStatic, wmCtlColorEdit:
		return w.controlColor(wParam)
	case wmDestroy:
		mainWindows.Delete(hwnd)
		w.mu.Lock()
		w.hwnd = 0
		w.mu.Unlock()
		deleteObject(w.font)
		deleteObject(w.uiFont)
		w.font = 0
		w.uiFont = 0
		procPostQuitMessage.Call(0)
		return 0
	}

	return defWindowProc(hwnd, message, wParam, lParam)
}

func mainEditSubclassProc(hwnd, message, wParam, lParam uintptr) uintptr {
	parent, _, _ := procGetParent.Call(hwnd)
	if parent != 0 {
		if value, ok := mainWindows.Load(parent); ok {
			w := value.(*Window)
			if message == wmKeyDown {
				if keyDown(vkControl) {
					switch wParam {
					case vkOEMPlus, vkAdd:
						w.zoom(1)
						return 0
					case vkOEMMinus, vkSubtract:
						w.zoom(-1)
						return 0
					case '0', vkNumpad0:
						w.resetZoom()
						return 0
					}
				}
				switch wParam {
				case vkReturn:
					postMessage(parent, wmCommand, idOK, 0)
					return 0
				case vkEscape:
					postMessage(parent, wmClose, 0, 0)
					return 0
				}
			}
			if message == wmMouseWheel && keyDown(vkControl) {
				delta := int16(wParam >> 16)
				if delta > 0 {
					w.zoom(1)
				} else if delta < 0 {
					w.zoom(-1)
				}
				return 0
			}
		}
	}

	if mainEditOldProc == 0 {
		return 0
	}
	return callWindowProc(mainEditOldProc, hwnd, message, wParam, lParam)
}

var (
	mainWindows     sync.Map
	mainEditOldProc uintptr
)
