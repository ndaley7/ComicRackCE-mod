#@Name Master UI
#@Description Launches the ComicRack-Util Master UI with Python 3.
#@Key Ctrl+Shift+M

import os
from System import Exception
from System.Diagnostics import Process, ProcessStartInfo
from System.IO import Path
from System.Windows.Forms import MessageBox, MessageBoxButtons, MessageBoxIcon


SCRIPT_PATH = r"E:\GIT\_CODEX\ComicRack-Util\master_ui.py"


def show_error(message):
    MessageBox.Show(message, "Master UI", MessageBoxButtons.OK, MessageBoxIcon.Error)


def start_python(executable, arguments):
    start_info = ProcessStartInfo()
    start_info.FileName = executable
    start_info.Arguments = arguments
    start_info.WorkingDirectory = Path.GetDirectoryName(SCRIPT_PATH)
    start_info.UseShellExecute = True
    Process.Start(start_info)


if not os.path.exists(SCRIPT_PATH):
    show_error("Could not find:\n\n%s" % SCRIPT_PATH)
else:
    try:
        start_python("py.exe", '-3 "%s"' % SCRIPT_PATH)
    except Exception:
        try:
            start_python("python.exe", '"%s"' % SCRIPT_PATH)
        except Exception as ex:
            show_error("Could not launch Master UI with Python 3.\n\n%s" % ex)
