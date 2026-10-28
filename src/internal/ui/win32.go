package ui

import (
	"errors"
	"runtime"
	"sync"
	"syscall"
	"unsafe"

	"golang.org/x/sys/windows"
)

const (
	wsOverlappedWindow   = 0x00CF0000
	wsPopup              = 0x80000000
	wsCaption            = 0x00C00000
	wsSysMenu            = 0x00080000
	wsChild              = 0x40000000
	wsVisible            = 0x10000000
	wsTabStop            = 0x00010000
	wsVScroll            = 0x00200000
	wsHScroll            = 0x00100000
	wsClipChildren       = 0x02000000
	wsExClientEdge       = 0x00000200
	wsExAppWindow        = 0x00040000
	wsExDialogModalFrame = 0x00000001
	wsExControlParent    = 0x00010000

	esLeft        = 0x0000
	esCenter      = 0x0001
	esMultiline   = 0x0004
	esAutoVScroll = 0x0040
	esAutoHScroll = 0x0080
	esNoHideSel   = 0x0100
	esReadOnly    = 0x0800

	bsPushButton    = 0x00000000
	bsDefPushButton = 0x00000001

	swShow       = 5
	swShowNormal = 1
	cwUseDefault = 0x80000000

	smCXScreen = 0
	smCYScreen = 1
	idcArrow   = 32512

	idEdit    = 1000
	idOpenLog = 1001
	idOK      = 1
	idCancel  = 2

	wmDestroy        = 0x0002
	wmSize           = 0x0005
	wmSetRedraw      = 0x000B
	wmGetTextLength  = 0x000E
	wmClose          = 0x0010
	wmGetMinMaxInfo  = 0x0024
	wmSetFont        = 0x0030
	wmKeyDown        = 0x0100
	wmCommand        = 0x0111
	wmCtlColorEdit   = 0x0133
	wmCtlColorStatic = 0x0138
	wmMouseWheel     = 0x020A
	wmDpiChanged     = 0x02E0
	wmAppend         = 0x8001
	wmDone           = 0x8002

	emSetSel       = 0x00B1
	emScrollCaret  = 0x00B7
	emReplaceSel   = 0x00C2
	emSetLimitText = 0x00C5
	emSetMargins   = 0x00D3

	ecLeftMargin  = 0x0001
	ecRightMargin = 0x0002

	mbOK              = 0x00000000
	mbIconError       = 0x00000010
	mbIconWarning     = 0x00000030
	mbIconInformation = 0x00000040
	mbSetForeground   = 0x00010000

	swpNoSize     = 0x0001
	swpNoZOrder   = 0x0004
	swpNoActivate = 0x0010

	vkControl  = 0x11
	vkReturn   = 0x0D
	vkEscape   = 0x1B
	vkOEMPlus  = 0xBB
	vkOEMMinus = 0xBD
	vkAdd      = 0x6B
	vkSubtract = 0x6D
	vkNumpad0  = 0x60

	defaultCharset   = 1
	cleartypeQuality = 5
	fwNormal         = 400
	ffModern         = 48
	fixedPitch       = 1

	opaque = 2

	gwlpWndProc = ^uintptr(3)
)

type wndClassEx struct {
	Size       uint32
	Style      uint32
	WndProc    uintptr
	ClsExtra   int32
	WndExtra   int32
	Instance   windows.Handle
	Icon       windows.Handle
	Cursor     windows.Handle
	Background windows.Handle
	MenuName   *uint16
	ClassName  *uint16
	IconSm     windows.Handle
}

type point struct {
	X int32
	Y int32
}

type rect struct {
	Left   int32
	Top    int32
	Right  int32
	Bottom int32
}

type minMaxInfo struct {
	Reserved     point
	MaxSize      point
	MaxPosition  point
	MinTrackSize point
	MaxTrackSize point
}

type msg struct {
	Hwnd    windows.Handle
	Message uint32
	WParam  uintptr
	LParam  uintptr
	Time    uint32
	Pt      point
	Private uint32
}

var (
	user32   = windows.NewLazySystemDLL("user32.dll")
	gdi32    = windows.NewLazySystemDLL("gdi32.dll")
	kernel32 = windows.NewLazySystemDLL("kernel32.dll")
	shell32  = windows.NewLazySystemDLL("shell32.dll")

	procRegisterClassExW     = user32.NewProc("RegisterClassExW")
	procCreateWindowExW      = user32.NewProc("CreateWindowExW")
	procDefWindowProcW       = user32.NewProc("DefWindowProcW")
	procDestroyWindow        = user32.NewProc("DestroyWindow")
	procPostQuitMessage      = user32.NewProc("PostQuitMessage")
	procGetMessageW          = user32.NewProc("GetMessageW")
	procTranslateMessage     = user32.NewProc("TranslateMessage")
	procDispatchMessageW     = user32.NewProc("DispatchMessageW")
	procIsDialogMessageW     = user32.NewProc("IsDialogMessageW")
	procShowWindow           = user32.NewProc("ShowWindow")
	procUpdateWindow         = user32.NewProc("UpdateWindow")
	procGetWindowTextW       = user32.NewProc("GetWindowTextW")
	procGetWindowTextLengthW = user32.NewProc("GetWindowTextLengthW")
	procGetClientRect        = user32.NewProc("GetClientRect")
	procMoveWindow           = user32.NewProc("MoveWindow")
	procGetWindowRect        = user32.NewProc("GetWindowRect")
	procGetSystemMetrics     = user32.NewProc("GetSystemMetrics")
	procEnableWindow         = user32.NewProc("EnableWindow")
	procSetFocus             = user32.NewProc("SetFocus")
	procHideCaret            = user32.NewProc("HideCaret")
	procSendMessageW         = user32.NewProc("SendMessageW")
	procPostMessageW         = user32.NewProc("PostMessageW")
	procSetWindowLongPtrW    = user32.NewProc("SetWindowLongPtrW")
	procMessageBoxW          = user32.NewProc("MessageBoxW")
	procSetWindowPos         = user32.NewProc("SetWindowPos")
	procLoadCursorW          = user32.NewProc("LoadCursorW")
	procLoadIconW            = user32.NewProc("LoadIconW")
	procGetDC                = user32.NewProc("GetDC")
	procReleaseDC            = user32.NewProc("ReleaseDC")
	procInvalidateRect       = user32.NewProc("InvalidateRect")
	procCallWindowProcW      = user32.NewProc("CallWindowProcW")
	procGetParent            = user32.NewProc("GetParent")
	procGetKeyState          = user32.NewProc("GetKeyState")
	procGetModuleHandleW     = kernel32.NewProc("GetModuleHandleW")
	procGetDeviceCaps        = gdi32.NewProc("GetDeviceCaps")
	procCreateFontW          = gdi32.NewProc("CreateFontW")
	procCreateSolidBrush     = gdi32.NewProc("CreateSolidBrush")
	procDeleteObject         = gdi32.NewProc("DeleteObject")
	procSetBkMode            = gdi32.NewProc("SetBkMode")
	procSetBkColor           = gdi32.NewProc("SetBkColor")
	procSetTextColor         = gdi32.NewProc("SetTextColor")
	procShellExecuteW        = shell32.NewProc("ShellExecuteW")

	mainClassName = utf16Ptr("ETSUMainWindow")
	dateClassName = utf16Ptr("ETSUDateWindow")

	mainWndProcCallback = syscall.NewCallback(mainWndProc)
	dateWndProcCallback = syscall.NewCallback(dateWndProc)
	mainEditSubclass    = syscall.NewCallback(mainEditSubclassProc)
	dateEditSubclass    = syscall.NewCallback(dateEditSubclassProc)

	uiOnce    sync.Once
	uiInitErr error
	hInstance uintptr
	uiBrush   uintptr
	cursor    uintptr

	bgColor = rgb(22, 22, 24)
	fgColor = rgb(232, 232, 232)
	errFont = errors.New("could not create UI font")
)

func rgb(r, g, b uint8) uintptr {
	return uintptr(r) | uintptr(g)<<8 | uintptr(b)<<16
}

func utf16Ptr(s string) *uint16 {
	p, _ := windows.UTF16PtrFromString(s)
	return p
}

func initUI() error {
	uiOnce.Do(func() {
		hInstance, _, _ = procGetModuleHandleW.Call(0)
		if hInstance == 0 {
			uiInitErr = errors.New("GetModuleHandleW failed")
			return
		}

		cursor, _, _ = procLoadCursorW.Call(0, idcArrow)
		uiBrush, _, _ = procCreateSolidBrush.Call(bgColor)
		if uiBrush == 0 {
			uiInitErr = errors.New("CreateSolidBrush failed")
			return
		}

		if err := registerClass(mainClassName, mainWndProcCallback); err != nil {
			uiInitErr = err
			return
		}
		if err := registerClass(dateClassName, dateWndProcCallback); err != nil {
			uiInitErr = err
			return
		}
	})
	return uiInitErr
}

func registerClass(name *uint16, proc uintptr) error {
	icon, _, _ := procLoadIconW.Call(hInstance, 1)
	wc := wndClassEx{
		Size:       uint32(unsafe.Sizeof(wndClassEx{})),
		WndProc:    proc,
		Instance:   windows.Handle(hInstance),
		Icon:       windows.Handle(icon),
		Cursor:     windows.Handle(cursor),
		Background: windows.Handle(uiBrush),
		ClassName:  name,
		IconSm:     windows.Handle(icon),
	}

	r, _, callErr := procRegisterClassExW.Call(uintptr(unsafe.Pointer(&wc)))
	runtime.KeepAlive(&wc)
	if r != 0 {
		return nil
	}
	if errno, ok := callErr.(syscall.Errno); ok {
		if errno == 1410 {
			return nil
		}
		if errno != 0 {
			return errno
		}
	}
	return errors.New("RegisterClassExW failed")
}

func systemDPI() uint32 {
	hdc, _, _ := procGetDC.Call(0)
	if hdc == 0 {
		return 96
	}
	defer procReleaseDC.Call(0, hdc)

	dpi, _, _ := procGetDeviceCaps.Call(hdc, 90)
	if dpi < 72 || dpi > 480 {
		return 96
	}
	return uint32(dpi)
}

func scale(v int32, dpi uint32) int32 {
	return v * int32(dpi) / 96
}

func createFont(face string, points int, dpi uint32) windows.Handle {
	height := -int32(points) * int32(dpi) / 72
	facePtr, err := windows.UTF16PtrFromString(face)
	if err != nil {
		return 0
	}

	pitch := uintptr(0)
	if face == "Consolas" {
		pitch = uintptr(ffModern | fixedPitch)
	}

	r, _, _ := procCreateFontW.Call(
		uintptr(height), 0, 0, 0, fwNormal, 0, 0, 0,
		defaultCharset, 0, 0, cleartypeQuality, pitch,
		uintptr(unsafe.Pointer(facePtr)),
	)
	runtime.KeepAlive(facePtr)
	return windows.Handle(r)
}

func defWindowProc(hwnd, message, wParam, lParam uintptr) uintptr {
	r, _, _ := procDefWindowProcW.Call(hwnd, message, wParam, lParam)
	return r
}

func destroyWindow(hwnd uintptr) {
	if hwnd != 0 {
		procDestroyWindow.Call(hwnd)
	}
}

func sendMessage(hwnd uintptr, message uint32, wParam, lParam uintptr) uintptr {
	r, _, _ := procSendMessageW.Call(hwnd, uintptr(message), wParam, lParam)
	return r
}

func postMessage(hwnd uintptr, message uint32, wParam, lParam uintptr) {
	if hwnd != 0 {
		procPostMessageW.Call(hwnd, uintptr(message), wParam, lParam)
	}
}

func getWindowText(hwnd uintptr) string {
	n, _, _ := procGetWindowTextLengthW.Call(hwnd)
	buf := make([]uint16, int(n)+1)
	if n > 0 {
		procGetWindowTextW.Call(hwnd, uintptr(unsafe.Pointer(&buf[0])), uintptr(len(buf)))
		runtime.KeepAlive(buf)
	}
	return windows.UTF16ToString(buf)
}

func moveWindow(hwnd uintptr, x, y, width, height int32, repaint bool) {
	repaintValue := uintptr(0)
	if repaint {
		repaintValue = 1
	}
	procMoveWindow.Call(hwnd, uintptr(x), uintptr(y), uintptr(width), uintptr(height), repaintValue)
}

func clientRect(hwnd uintptr) rect {
	var r rect
	procGetClientRect.Call(hwnd, uintptr(unsafe.Pointer(&r)))
	runtime.KeepAlive(&r)
	return r
}

func windowRect(hwnd uintptr) rect {
	var r rect
	procGetWindowRect.Call(hwnd, uintptr(unsafe.Pointer(&r)))
	runtime.KeepAlive(&r)
	return r
}

func centerWindow(hwnd uintptr) {
	r := windowRect(hwnd)
	screenWidth, _, _ := procGetSystemMetrics.Call(smCXScreen)
	screenHeight, _, _ := procGetSystemMetrics.Call(smCYScreen)
	x := (int32(screenWidth) - (r.Right - r.Left)) / 2
	y := (int32(screenHeight) - (r.Bottom - r.Top)) / 2
	procSetWindowPos.Call(hwnd, 0, uintptr(x), uintptr(y), 0, 0, swpNoSize|swpNoZOrder|swpNoActivate)
}

func showMessage(owner uintptr, title, text string, flags uint32) {
	titlePtr, _ := windows.UTF16PtrFromString(title)
	textPtr, _ := windows.UTF16PtrFromString(text)
	procMessageBoxW.Call(owner, uintptr(unsafe.Pointer(textPtr)), uintptr(unsafe.Pointer(titlePtr)), uintptr(flags))
	runtime.KeepAlive(titlePtr)
	runtime.KeepAlive(textPtr)
}

func ShowError(title, text string) {
	showMessage(0, title, text, mbOK|mbIconError|mbSetForeground)
}

func ShowInfo(title, text string) {
	showMessage(0, title, text, mbOK|mbIconInformation|mbSetForeground)
}

func openPath(path string) {
	verb, _ := windows.UTF16PtrFromString("open")
	target, _ := windows.UTF16PtrFromString(path)
	r, _, _ := procShellExecuteW.Call(
		0,
		uintptr(unsafe.Pointer(verb)),
		uintptr(unsafe.Pointer(target)),
		0,
		0,
		swShowNormal,
	)
	runtime.KeepAlive(verb)
	runtime.KeepAlive(target)
	if r <= 32 {
		ShowError("ETSU", "Could not open log:\n"+path)
	}
}

func enableWindow(hwnd uintptr, enabled bool) {
	value := uintptr(0)
	if enabled {
		value = 1
	}
	procEnableWindow.Call(hwnd, value)
}

func setFocus(hwnd uintptr) {
	procSetFocus.Call(hwnd)
}

func invalidateRect(hwnd uintptr, erase bool) {
	value := uintptr(0)
	if erase {
		value = 1
	}
	procInvalidateRect.Call(hwnd, 0, value)
}

func keyDown(vk uintptr) bool {
	r, _, _ := procGetKeyState.Call(vk)
	return int16(r) < 0
}

func deleteObject(h windows.Handle) {
	if h != 0 {
		procDeleteObject.Call(uintptr(h))
	}
}

func callWindowProc(proc, hwnd, message, wParam, lParam uintptr) uintptr {
	r, _, _ := procCallWindowProcW.Call(proc, hwnd, message, wParam, lParam)
	return r
}

func callError(err error, fallback string) error {
	if errno, ok := err.(syscall.Errno); ok && errno != 0 {
		return errno
	}
	if err != nil {
		return err
	}
	return errors.New(fallback)
}
