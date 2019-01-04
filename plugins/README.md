#Publisher contracts

This folder contains the contracts used by plugins to communicate with the host `go-between` process.

Go-between uses the hashicorp [go-plugin](https://github.com/hashicorp/go-plugin) framework and gRPC to communicate with plugins.

The go-plugin handshake contract that plugins must validate is

```
    ProtocolVersion:  1,
	MagicCookieKey:   "PUBLISHER_PLUGIN"
	MagicCookieValue: "naveego"
```

## Docs:

- [OAuth flow](./oath.md)
