# About

SyncingShip is a .NET implementation of a basic file-sharing protocol, created for Networking class at Avans Breda, period IDH14.

For information about the protocol, see [this page](https://idh14.github.io/protocol/).

## Disclaimer

The protocol itself is lacking. It is also only suitable for sharing small files (about 50 MB at most). This is because requests and responses are done in text format, JSON to be precise, where the binary file data is encoded to base64. That said, it was never meant to be perfect, just good enough for the assignment's requirements.