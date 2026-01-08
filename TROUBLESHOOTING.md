# Troubleshooting

## My server host won't let me use anything but the official release

See [the hosted server installation instructions](./INSTALL.md#hosted-server).

You shouldn't have to disable `enforceMod`.

## Version mismatch when connecting to server

Either the client or the server is on the wrong version, or is missing Valheim Plus altogether.

### Not installed
If you have a version mismatch between `Valheim 0.214.300@0.9.9.13` and `Valheim 0.214.300`, that means that the latter does not have the mod correctly installed, and you should go through the steps in the [installation instructions](./INSTALL.md) again.

### Wrong Version
If you have a version mismatch between `Valheim 0.214.300@0.9.9.13` and `Valheim 0.214.300@0.9.9.12`, then the latter needs to update Valheim Plus, and you should go through the steps in the [installation instructions](./INSTALL.md) again

### Wrong Version, but probably not your fault
If you have a version mismatch between a client correctly on `Valheim 0.214.300@0.9.9.13` and a server on an old version like `Valheim 0.214.300@0.9.9.11`, *but you did already install the newest version*, then your issue is probably actually ["My server host won't let me use anything but the official release"](#my-server-host-wont-let-me-use-anything-but-the-official-release). This is because there is not a community fix that has the version `0.9.9.11`. *Well, there are some `0.9.9.11`s out there that do work if you went through special steps to install it, but if you don't know what I'm talking about, then see the previous sentence.*

