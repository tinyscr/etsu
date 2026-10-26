package install

import (
	"errors"

	"golang.org/x/sys/windows"
	"golang.org/x/sys/windows/registry"
)

const menuRoot = `Software\Classes\*\shell\etsu`

type menuItem struct {
	key   string
	label string
	verb  string
}

var menuItems = []menuItem{
	{"a_read", "&Read metadata...", "read"},
	{"b_clean", "&Clean metadata...", "clean"},
	{"c_date", "&Set date...", "date"},
}

func registerMenu(exePath string) error {
	key, _, err := registry.CreateKey(registry.CURRENT_USER, menuRoot, registry.WRITE)
	if err != nil {
		return err
	}
	defer key.Close()

	if err := key.SetStringValue("MUIVerb", "etsu"); err != nil {
		return err
	}
	if err := key.SetStringValue("subcommands", ""); err != nil {
		return err
	}
	if err := key.SetStringValue("MultiSelectModel", "Single"); err != nil {
		return err
	}

	for _, item := range menuItems {
		path := menuRoot + `\shell\` + item.key
		child, _, err := registry.CreateKey(registry.CURRENT_USER, path, registry.WRITE)
		if err != nil {
			return err
		}
		if err := child.SetStringValue("MUIVerb", item.label); err != nil {
			child.Close()
			return err
		}
		if err := child.SetStringValue("MultiSelectModel", "Single"); err != nil {
			child.Close()
			return err
		}
		child.Close()

		command, _, err := registry.CreateKey(registry.CURRENT_USER, path+`\command`, registry.WRITE)
		if err != nil {
			return err
		}
		if err := command.SetStringValue("", `"`+exePath+`" `+item.verb+` "%L"`); err != nil {
			command.Close()
			return err
		}
		command.Close()
	}

	return nil
}

func unregisterMenu() (bool, error) {
	key, err := registry.OpenKey(registry.CURRENT_USER, menuRoot, registry.READ)
	if errors.Is(err, windows.ERROR_FILE_NOT_FOUND) {
		return false, nil
	}
	if err != nil {
		return false, err
	}
	key.Close()

	if err := deleteTree(menuRoot); err != nil {
		return false, err
	}
	return true, nil
}

func deleteTree(path string) error {
	key, err := registry.OpenKey(registry.CURRENT_USER, path, registry.READ)
	if err != nil {
		return err
	}
	names, _ := key.ReadSubKeyNames(-1)
	key.Close()

	for _, name := range names {
		if err := deleteTree(path + `\` + name); err != nil {
			return err
		}
	}
	return registry.DeleteKey(registry.CURRENT_USER, path)
}
