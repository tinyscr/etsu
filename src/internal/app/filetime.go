package app

import (
	"time"

	"golang.org/x/sys/windows"
)

func SetCreationTime(path string, t time.Time) error {
	name, err := windows.UTF16PtrFromString(path)
	if err != nil {
		return err
	}

	handle, err := windows.CreateFile(
		name,
		windows.FILE_WRITE_ATTRIBUTES,
		windows.FILE_SHARE_READ|windows.FILE_SHARE_WRITE|windows.FILE_SHARE_DELETE,
		nil,
		windows.OPEN_EXISTING,
		windows.FILE_ATTRIBUTE_NORMAL,
		0,
	)
	if err != nil {
		return err
	}
	defer windows.CloseHandle(handle)

	ft := windows.NsecToFiletime(t.UnixNano())
	return windows.SetFileTime(handle, &ft, nil, nil)
}
