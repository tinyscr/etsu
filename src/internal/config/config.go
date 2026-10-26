package config

import (
	"os"
	"strconv"
	"strings"
)

type File struct {
	sections map[string]map[string]string
}

func Load(path string) *File {
	f := &File{sections: make(map[string]map[string]string)}
	data, err := os.ReadFile(path)
	if err != nil {
		return f
	}

	var current map[string]string
	for _, raw := range strings.Split(string(data), "\n") {
		line := strings.TrimSpace(strings.TrimSuffix(raw, "\r"))
		if line == "" || strings.HasPrefix(line, "#") || strings.HasPrefix(line, ";") {
			continue
		}

		if strings.HasPrefix(line, "[") && strings.HasSuffix(line, "]") {
			name := strings.ToLower(strings.TrimSpace(line[1 : len(line)-1]))
			current = f.sections[name]
			if current == nil {
				current = make(map[string]string)
				f.sections[name] = current
			}
			continue
		}

		eq := strings.IndexByte(line, '=')
		if eq <= 0 || current == nil {
			continue
		}
		current[strings.ToLower(strings.TrimSpace(line[:eq]))] = strings.TrimSpace(line[eq+1:])
	}
	return f
}

func (f *File) lookup(section, key string) (string, bool) {
	s := f.sections[strings.ToLower(section)]
	if s == nil {
		return "", false
	}
	v, ok := s[strings.ToLower(key)]
	return v, ok
}

func (f *File) String(section, key, def string) string {
	if v, ok := f.lookup(section, key); ok {
		return v
	}
	return def
}

func (f *File) Bool(section, key string, def bool) bool {
	v, ok := f.lookup(section, key)
	if !ok {
		return def
	}
	return strings.EqualFold(v, "true")
}

func (f *File) Int(section, key string, def int) int {
	v, ok := f.lookup(section, key)
	if !ok {
		return def
	}
	n, err := strconv.Atoi(v)
	if err != nil || n < 0 {
		return def
	}
	return n
}
