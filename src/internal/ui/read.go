package ui

import (
	"strings"

	"github.com/lxn/walk"
)

type metadataRow struct {
	tag   string
	value string
}

type metadataModel struct {
	walk.TableModelBase
	rows []metadataRow
}

func (m *metadataModel) RowCount() int {
	return len(m.rows)
}

func (m *metadataModel) Value(row, col int) interface{} {
	if row < 0 || row >= len(m.rows) {
		return ""
	}
	switch col {
	case 0:
		return m.rows[row].tag
	case 1:
		return m.rows[row].value
	default:
		return ""
	}
}

func (m *metadataModel) Add(tag, value string) {
	m.rows = append(m.rows, metadataRow{tag: tag, value: value})
	last := len(m.rows) - 1
	m.PublishRowsInserted(last, last)
}

func (m *metadataModel) Text() string {
	var b strings.Builder
	for _, row := range m.rows {
		b.WriteString(row.tag)
		b.WriteString(": ")
		b.WriteString(row.value)
		b.WriteString("\r\n")
	}
	return b.String()
}

func parseMetadataLine(line string) (tag, value string, ok bool) {
	if strings.HasPrefix(line, "[") || strings.HasPrefix(line, "File: ") {
		return "", "", false
	}

	i := strings.Index(line, ": ")
	if i <= 0 {
		return "", "", false
	}

	tag = strings.TrimSpace(line[:i])
	value = strings.TrimSpace(line[i+2:])
	if tag == "" {
		return "", "", false
	}
	return tag, value, true
}
