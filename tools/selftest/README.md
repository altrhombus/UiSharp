# Self-testing UiSharp

UiSharp is a front-end for ConfigMgr task sequences, which means the code that
matters most runs in the least observable place there is: a WinPE boot image, in
the middle of somebody's deployment, with no debugger and one log file to show
for it.

Every serious defect found in this port so far has lived in exactly that path —
a log directory treated as a file, variables enumerated from the wrong place, a
crash before the first dialog. None of it is reachable from a unit test, because
none of it exists until there is a real task-sequence environment on the other
side of the COM object.

So there are two instruments. One runs unattended and checks what a machine can
answer for itself; the other needs a person and checks what only a person can
judge.

## The unattended self-test

```
UiSharp.exe /selftest
```

Runs about fifty checks and writes a report next to the log, so whatever collects
SMSTS logs collects the report too. Nothing is displayed: it is safe to run
inside a real deployment, and safe to run unattended.

| Switch | Meaning |
|---|---|
| `/selftest` | Run the checks instead of a configuration. |
| `/selftestreport:<path>` | Write the report somewhere specific. A directory is fine — the file is named for you. Implies `/selftest`. |

Exit codes: `0` if everything passed, `4` if anything failed, `3` if the runtime
crashed outright.

What it covers:

- **Environment** — where the log went, whether it can be written, what the
  runtime thinks it is.
- **Task sequence** — setting, reading, testing and *enumerating* variables
  through the live environment. Enumeration is the one that has shipped broken:
  a run that cannot enumerate writes an empty variable file and says nothing.
- **Variable files** — the JSON round trip, including the values that break a
  line-per-variable format, and that a damaged file is ignored rather than fatal.
- **Condition engine** — a sample of conditions with known answers, evaluated by
  the engine that actually shipped. Trimming can change what reflection finds,
  and a condition that quietly changes meaning is how a deployment images the
  wrong thing.
- **Action pipeline** — that every action type is still discoverable, and that a
  small configuration produces exactly the variables it should.
- **Platform** — the registry, WMI, and the `X` variables collected by
  `DefaultValues`, with the interesting ones printed so you can see how this
  machine was classified.

The report marks whether the run was inside a task sequence. A clean run outside
one proves much less, and says so at the top.

### In a task sequence

Add a **Run Command Line** step:

```
UiSharp.exe /selftest
```

Let it continue on error if you want the deployment to proceed regardless; the
report is written either way. `UiSharp_selftest.txt` will be beside `UiSharp.log`
in `%_SMSTSLogPath%`.

## The interactive self-test

`UiSharp-SelfTest.xml` in this directory is an ordinary configuration:

```
UiSharp.exe /config:UiSharp-SelfTest.xml
```

It shows every dialog UiSharp can display, one after another, and each screen
says what to look at. Run it in the boot image, on the hardware you deploy to,
at the resolution that hardware actually boots at — a dialog that looks right on
a 4K desktop can be unreadable at 1024x768 in WinPE with a different font set.

It writes what you entered to `%temp%\UiSharp-SelfTest.dat` and shows it back at
the end, so the run leaves a record of what was seen rather than only that it
finished. The password field is cleared before anything is written.

Nothing in it needs a network, a task sequence, or a ConfigMgr site.

The file is covered by tests — that it loads, that every action type it names
exists, that it still exercises every dialog, that its conditions evaluate — so
a trip to the lab is not wasted on a typo. What the tests cannot tell you is
whether any of it looked right.
