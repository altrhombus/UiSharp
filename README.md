# UIPlusPlus
 UI++

Initial public open-source code release: December 14, 2024

This is all of the UI++ source code in all of its glory and the result of my hardwork over the past 15 or so years and now available for you to do whatever you want with it. It's been more than two years since I last built this solution so I really don't remember everything but will happily help and aid in anyway that I can to help UI++ continue to exist and help those that need it.

Short brain dump of what's required:
- Visual Studio 2022
- Visual C++ with MFC static libraries
- curl libraries (download at https://curl.se/download.html). This gets a bit tricky. UI++ version 3.0.3.0 (the last version I built and published and included here) uses curl version 7.83.1.
  1. Download the curl source zip and extract.
  2. Build the libraies for use during linking. This needs to be done four times depending on your intent, once for each platform type (x86 or x64) and once for each build type (Debug or Release). This is the command line that I used for the Debug X64 version: "nmake /f Makefile.vc VC=14 mode=static ENABLE_SSPI=yes ENABLE_IPV6=yes ENABLE_SCHANNEL=yes ENABLE_UNICODE=yes machine=x64 DEBUG=yes". Just change the machine and DEBUG parameters appropriately to build the applicable libraries.
  3. Update the Additional Library Directories Under General->Linked in the UI++ project to point to the newly compiled libraries so they can be properly linked durung the build of the solution.
- The binaries (FTWCMLog.dll, FTWldap.dll, and UI++.exe) will get dumped into the appropriate sub-folder of the solution Build folder.

I'm sure there's more and I'll add it here if and when it comes up. Please reach out if you have questions, concerns, comments, or need help.
