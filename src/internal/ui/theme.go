package ui

import (
	"syscall"
	"unsafe"

	"github.com/lxn/walk"
	"github.com/lxn/win"
	"golang.org/x/sys/windows"
	"golang.org/x/sys/windows/registry"
)

const (
	// Undocumented uxtheme exports used by the Windows 10/11 dark-mode shell.
	ordAllowDarkModeForWindow = 133
	ordSetPreferredAppMode    = 135
	ordFlushMenuThemes        = 136

	// DWMWA_USE_IMMERSIVE_DARK_MODE; 19 is the pre-20H1 attribute.
	dwmwaUseImmersiveDarkMode         = 20
	dwmwaUseImmersiveDarkModeBefore20 = 19

	// Owner-draw state flags. lxn/win has these shifted, so keep local copies.
	odtButton   = 4
	odsSelected = 0x0001
	odsDisabled = 0x0004
	odsFocus    = 0x0010
	odsDefault  = 0x0020
	bsTypeMask  = 0x000F
)

var (
	uxtheme = windows.NewLazySystemDLL("uxtheme.dll")
	dwmapi  = windows.NewLazySystemDLL("dwmapi.dll")
	user32  = windows.NewLazySystemDLL("user32.dll")

	procDwmSetWindowAttribute = dwmapi.NewProc("DwmSetWindowAttribute")
	procFillRect              = user32.NewProc("FillRect")
	procGetWindowTextW        = user32.NewProc("GetWindowTextW")

	enumChildProc = syscall.NewCallback(func(hwnd, _ uintptr) uintptr {
		currentTheme().applyControl(win.HWND(hwnd))
		return 1
	})

	tableChildProc = syscall.NewCallback(func(hwnd, _ uintptr) uintptr {
		currentTheme().styleTableChild(win.HWND(hwnd))
		return 1
	})

	drawItemProc = syscall.NewCallback(drawItemWndProc)

	drawItemParents = make(map[win.HWND]uintptr)
)

type theme struct {
	dark         bool
	highContrast bool

	window        walk.Color
	surface       walk.Color
	text          walk.Color
	border        walk.Color
	accent        walk.Color
	button        walk.Color
	buttonPressed walk.Color
	disabledText  walk.Color

	windowBrush  *walk.SolidColorBrush
	surfaceBrush *walk.SolidColorBrush
	windowHBrush win.HBRUSH
}

var activeTheme *theme

func currentTheme() *theme {
	if activeTheme == nil {
		activeTheme = loadTheme()
	}
	return activeTheme
}

// InitTheme must run before the first window is created. It tells uxtheme
// which app mode to use; the window-level flags are applied after creation.
func InitTheme() {
	t := currentTheme()
	switch {
	case t.highContrast:
		setPreferredAppMode(0)
	case t.dark:
		setPreferredAppMode(2)
	default:
		setPreferredAppMode(3)
	}
	if t.dark && !t.highContrast {
		flushMenuThemes()
	}
}

func loadTheme() *theme {
	dark, highContrast := systemTheme()
	t := &theme{dark: dark && !highContrast, highContrast: highContrast}
	if !t.dark {
		return t
	}

	t.window = walk.RGB(32, 32, 32)
	t.surface = walk.RGB(43, 43, 43)
	t.text = walk.RGB(243, 243, 243)
	t.border = walk.RGB(64, 64, 64)
	t.accent = walk.Color(win.GetSysColor(win.COLOR_HOTLIGHT))
	t.button = walk.RGB(45, 45, 45)
	t.buttonPressed = walk.RGB(38, 38, 38)
	t.disabledText = walk.RGB(140, 140, 140)

	t.windowBrush, _ = walk.NewSolidColorBrush(t.window)
	t.surfaceBrush, _ = walk.NewSolidColorBrush(t.surface)
	t.windowHBrush = win.CreateBrushIndirect(&win.LOGBRUSH{
		LbStyle: win.BS_SOLID,
		LbColor: win.COLORREF(t.window),
	})
	return t
}

func systemTheme() (dark, highContrast bool) {
	var hc win.HIGHCONTRAST
	hc.CbSize = uint32(unsafe.Sizeof(hc))
	if win.SystemParametersInfo(win.SPI_GETHIGHCONTRAST, hc.CbSize, unsafe.Pointer(&hc), 0) &&
		hc.DwFlags&win.HCF_HIGHCONTRASTON != 0 {
		return false, true
	}

	key, err := registry.OpenKey(
		registry.CURRENT_USER,
		`Software\Microsoft\Windows\CurrentVersion\Themes\Personalize`,
		registry.QUERY_VALUE,
	)
	if err != nil {
		return false, false
	}
	defer key.Close()

	value, _, err := key.GetIntegerValue("AppsUseLightTheme")
	if err != nil {
		return false, false
	}
	return value == 0, false
}

// ApplyTheme styles a form and every walk widget inside it. Light mode and
// high contrast are left alone so Windows owns the colors.
func ApplyTheme(form walk.Form) {
	t := currentTheme()
	if t == nil || !t.dark || form == nil {
		return
	}

	if t.windowBrush != nil {
		form.SetBackground(t.windowBrush)
	}

	hwnd := form.Handle()
	t.applyWindow(hwnd, true)
	win.EnumChildWindows(hwnd, enumChildProc, 0)
	children := form.Children()
	for i := 0; i < children.Len(); i++ {
		t.styleWidget(children.At(i))
	}
}

func (t *theme) styleWidget(w walk.Widget) {
	if w == nil {
		return
	}

	t.applyControl(w.Handle())

	switch v := w.(type) {
	case *walk.Label:
		v.SetTextColor(t.text)
	case *walk.TextEdit:
		v.SetTextColor(t.text)
		if t.surfaceBrush != nil {
			v.SetBackground(t.surfaceBrush)
		}
	case *walk.LineEdit:
		v.SetTextColor(t.text)
		if t.surfaceBrush != nil {
			v.SetBackground(t.surfaceBrush)
		}
	case *walk.DateEdit:
		if t.surfaceBrush != nil {
			v.SetBackground(t.surfaceBrush)
		}
	case *walk.TableView:
		t.styleTable(v)
	case *walk.ProgressBar:
		// The themed progress bar keeps its light trough even in dark mode.
		disableWindowTheme(v.Handle())
		v.SendMessage(win.PBM_SETBKCOLOR, 0, uintptr(win.COLORREF(t.surface)))
		v.SendMessage(win.PBM_SETBARCOLOR, 0, uintptr(win.COLORREF(t.accent)))
	case *walk.PushButton:
		t.ownerDrawButton(v)
	}

	if c, ok := w.(walk.Container); ok {
		children := c.Children()
		for i := 0; i < children.Len(); i++ {
			t.styleWidget(children.At(i))
		}
	}
}

func (t *theme) styleTable(table *walk.TableView) {
	if t.surfaceBrush != nil {
		table.SetBackground(t.surfaceBrush)
	}
	win.EnumChildWindows(table.Handle(), tableChildProc, 0)
}

func (t *theme) styleTableChild(hwnd win.HWND) {
	var buf [64]uint16
	n, err := win.GetClassName(hwnd, &buf[0], len(buf))
	if err != nil || n <= 0 {
		return
	}

	switch windows.UTF16ToString(buf[:n]) {
	case "SysListView32":
		win.SendMessage(hwnd, win.LVM_SETBKCOLOR, 0, uintptr(win.COLORREF(t.surface)))
		win.SendMessage(hwnd, win.LVM_SETTEXTCOLOR, 0, uintptr(win.COLORREF(t.text)))
		win.SendMessage(hwnd, win.LVM_SETTEXTBKCOLOR, 0, uintptr(win.COLORREF(t.surface)))
	}
}

func (t *theme) applyWindow(hwnd win.HWND, topLevel bool) {
	if hwnd == 0 {
		return
	}
	t.applyControl(hwnd)
	if topLevel {
		setDarkTitleBar(hwnd, true)
	}
}

func (t *theme) applyControl(hwnd win.HWND) {
	if hwnd == 0 || !t.dark {
		return
	}
	allowDarkModeForWindow(hwnd, true)
	setWindowTheme(hwnd, "DarkMode_Explorer")
}

func setPreferredAppMode(mode uintptr) {
	proc, err := windows.GetProcAddressByOrdinal(windows.Handle(uxtheme.Handle()), ordSetPreferredAppMode)
	if err != nil {
		return
	}
	syscall.SyscallN(proc, mode)
}

func flushMenuThemes() {
	proc, err := windows.GetProcAddressByOrdinal(windows.Handle(uxtheme.Handle()), ordFlushMenuThemes)
	if err != nil {
		return
	}
	syscall.SyscallN(proc)
}

func allowDarkModeForWindow(hwnd win.HWND, allow bool) {
	proc, err := windows.GetProcAddressByOrdinal(windows.Handle(uxtheme.Handle()), ordAllowDarkModeForWindow)
	if err != nil {
		return
	}
	var value uintptr
	if allow {
		value = 1
	}
	syscall.SyscallN(proc, uintptr(hwnd), value)
}

func setWindowTheme(hwnd win.HWND, name string) {
	ptr, err := windows.UTF16PtrFromString(name)
	if err != nil {
		return
	}
	win.SetWindowTheme(hwnd, ptr, nil)
}

func disableWindowTheme(hwnd win.HWND) {
	empty, err := windows.UTF16PtrFromString("")
	if err != nil {
		return
	}
	win.SetWindowTheme(hwnd, empty, empty)
}

func setDarkTitleBar(hwnd win.HWND, dark bool) {
	if err := procDwmSetWindowAttribute.Find(); err != nil {
		return
	}

	var value int32
	if dark {
		value = 1
	}
	size := uintptr(unsafe.Sizeof(value))

	hr, _, _ := procDwmSetWindowAttribute.Call(
		uintptr(hwnd),
		dwmwaUseImmersiveDarkMode,
		uintptr(unsafe.Pointer(&value)),
		size,
	)
	if int32(hr) != 0 {
		procDwmSetWindowAttribute.Call(
			uintptr(hwnd),
			dwmwaUseImmersiveDarkModeBefore20,
			uintptr(unsafe.Pointer(&value)),
			size,
		)
	}
}

func (t *theme) ownerDrawButton(button *walk.PushButton) {
	hwnd := button.Handle()
	if hwnd == 0 {
		return
	}

	style := uint32(win.GetWindowLong(hwnd, win.GWL_STYLE))
	if style&bsTypeMask == win.BS_OWNERDRAW {
		return
	}

	style = style&^uint32(bsTypeMask) | win.BS_OWNERDRAW
	win.SetWindowLong(hwnd, win.GWL_STYLE, int32(style))
	win.SetWindowPos(
		hwnd, 0, 0, 0, 0, 0,
		win.SWP_NOMOVE|win.SWP_NOSIZE|win.SWP_NOZORDER|win.SWP_FRAMECHANGED,
	)

	if parent := win.GetParent(hwnd); parent != 0 {
		subclassDrawItem(parent)
	}
	win.InvalidateRect(hwnd, nil, true)
}

func subclassDrawItem(hwnd win.HWND) {
	if hwnd == 0 {
		return
	}
	if _, ok := drawItemParents[hwnd]; ok {
		return
	}

	old := win.SetWindowLongPtr(hwnd, win.GWLP_WNDPROC, drawItemProc)
	if old != 0 {
		drawItemParents[hwnd] = old
	}
}

func drawItemWndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr {
	if msg == win.WM_DRAWITEM && drawOwnerButton(lParam) {
		return 1
	}

	old, ok := drawItemParents[hwnd]
	if !ok || old == 0 {
		return win.DefWindowProc(hwnd, msg, wParam, lParam)
	}
	if msg == win.WM_NCDESTROY {
		delete(drawItemParents, hwnd)
		win.SetWindowLongPtr(hwnd, win.GWLP_WNDPROC, old)
	}
	return win.CallWindowProc(old, hwnd, msg, wParam, lParam)
}

func drawOwnerButton(lParam uintptr) bool {
	if lParam == 0 {
		return false
	}

	item := (*win.DRAWITEMSTRUCT)(unsafe.Pointer(lParam))
	if item.CtlType != odtButton {
		return false
	}
	t := currentTheme()
	if t == nil || !t.dark {
		return false
	}

	t.drawButton(item)
	return true
}

func (t *theme) drawButton(item *win.DRAWITEMSTRUCT) {
	hdc := item.HDC
	rc := item.RcItem

	// Fill the corners with the window color before drawing the rounded shape.
	if t.windowHBrush != 0 {
		procFillRect.Call(
			uintptr(hdc),
			uintptr(unsafe.Pointer(&rc)),
			uintptr(t.windowHBrush),
		)
	}

	background := t.button
	if item.ItemState&odsSelected != 0 {
		background = t.buttonPressed
	}
	border := t.border
	if item.ItemState&(odsFocus|odsDefault) != 0 {
		border = t.accent
	}

	brush := win.CreateBrushIndirect(&win.LOGBRUSH{
		LbStyle: win.BS_SOLID,
		LbColor: win.COLORREF(background),
	})
	pen := win.ExtCreatePen(
		win.PS_SOLID,
		1,
		&win.LOGBRUSH{LbStyle: win.BS_SOLID, LbColor: win.COLORREF(border)},
		0,
		nil,
	)
	if brush == 0 || pen == 0 {
		if brush != 0 {
			win.DeleteObject(win.HGDIOBJ(brush))
		}
		if pen != 0 {
			win.DeleteObject(win.HGDIOBJ(pen))
		}
		return
	}
	defer win.DeleteObject(win.HGDIOBJ(brush))
	defer win.DeleteObject(win.HGDIOBJ(pen))

	oldBrush := win.SelectObject(hdc, win.HGDIOBJ(brush))
	oldPen := win.SelectObject(hdc, win.HGDIOBJ(pen))
	win.RoundRect(hdc, rc.Left+1, rc.Top+1, rc.Right-1, rc.Bottom-1, 8, 8)
	win.SelectObject(hdc, oldBrush)
	win.SelectObject(hdc, oldPen)

	text := windowText(item.HwndItem)
	if text == "" {
		return
	}
	textColor := t.text
	if item.ItemState&odsDisabled != 0 {
		textColor = t.disabledText
	}

	oldBkMode := win.SetBkMode(hdc, win.TRANSPARENT)
	oldTextColor := win.SetTextColor(hdc, win.COLORREF(textColor))
	textRect := rc
	if item.ItemState&odsSelected != 0 {
		textRect.Top++
		textRect.Bottom++
	}
	ptr, err := windows.UTF16PtrFromString(text)
	if err == nil {
		win.DrawTextEx(
			hdc,
			ptr,
			-1,
			&textRect,
			win.DT_CENTER|win.DT_VCENTER|win.DT_SINGLELINE|win.DT_END_ELLIPSIS,
			nil,
		)
	}
	win.SetTextColor(hdc, oldTextColor)
	win.SetBkMode(hdc, oldBkMode)
}

func windowText(hwnd win.HWND) string {
	var buf [256]uint16
	n, _, _ := procGetWindowTextW.Call(
		uintptr(hwnd),
		uintptr(unsafe.Pointer(&buf[0])),
		uintptr(len(buf)),
	)
	if n == 0 {
		return ""
	}
	return windows.UTF16ToString(buf[:int(n)])
}
