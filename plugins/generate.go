package plugins

import (
	"os"
	"os/exec"
	"path/filepath"
	"runtime"
)

// PublisherProtocolVersion is the version of the Publisher protocol.
const PublisherProtocolVersion = 1

// PublisherMagicCookieKey is the env variable plugins should check for.
const PublisherMagicCookieKey = "PUBLISHER_PLUGIN"

// PublisherMagicCookieValue is the env variable value plugins should check for.
const PublisherMagicCookieValue = "naveego"

// GeneratePublisher generates a golang publisher implementation from the publisher.proto file.
// This file and method is mostly provided to allow this package to be pulled in using go get.
func GeneratePublisher(toDir string) error {
	_, path, _, _ := runtime.Caller(0)
	pluginsDir := filepath.Dir(path)
	cmd := exec.Command("protoc",
		"-I",
		pluginsDir,
		"--go_out=plugins=grpc:"+toDir,
		"publisher.proto")
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr

	err := cmd.Run()
	return err
}

// GenerateManagement generates the implementation from the management.proto file.
// This file and method is mostly provided to allow this package to be pulled in using go get.
func GenerateManagement(toDir string) error {
	_, path, _, _ := runtime.Caller(0)
	pluginsDir := filepath.Dir(path)
	cmd := exec.Command("protoc",
		"-I",
		pluginsDir,
		"--go_out=plugins=grpc:"+toDir,
		"management.proto")
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr

	err := cmd.Run()
	return err
}
