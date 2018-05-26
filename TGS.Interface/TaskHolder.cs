using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;

namespace TGS.Interface
{
	public class TaskHolder : ITaskHolder
	{
		/// <inheritdoc />
		public string Name { get; }
		/// <inheritdoc />
		public string Command { get; }
		/// <inheritdoc />
		public IList<string> ArgsList { get; }
		/// <inheritdoc />
		public bool IsShellTask { get; }
		/// <summary>
		/// A string representation of the argument list. To be given out in
		/// <see cref="SetupStartInfo"/>.
		/// </summary>
		public string ArgsString { get; }

		public TaskHolder(JToken data)
		{
			ValidateInitialData(data);

			Name = (data["name"]).ToObject<string>();
			Command = (data["command"]).ToObject<string>();

			ArgsList = new List<string>();
			foreach (var arg in (JArray)(data["args"]))
			{
				ArgsList.Add(arg.ToObject<string>());
			}

			ArgsString = ConstructArgsString();

			IsShellTask = (data["isShell"]).ToObject<bool>();
		}

		private string ConstructArgsString()
		{
			if (ArgsList.Count > 0)
			{
				string args = "";
				foreach (var arg in ArgsList)
				{
					args += $" {arg}";
				}

				return args;
			}
			else
			{
				return string.Empty;
			}
		}

		private void ValidateInitialData(JToken data)
		{
			var keys = new List<string> {"name", "command", "args", "isShell"};

			if (!data.HasValues)
			{
				throw new ArgumentException("Schema has no values.");
			}

			foreach (var key in keys)
			{
				if (data[key] == null)
				{
					throw new ArgumentException($"Key {key} not found in schema.");
				}
			}
		}

		/// <inheritdoc />
		public void RunTask(DirectoryInfo workingDir, int timeout = Int32.MaxValue)
		{
			string stdOut;
			string stdErr;
			bool timedOut;
			int exitCode = 0;
			using (var proc = new Process())
			{
				proc.StartInfo = SetupStartInfo(workingDir);

				using (StreamReader reader = proc.StandardOutput)
					stdOut = reader.ReadToEnd();
				using (StreamReader reader = proc.StandardError)
					stdErr = reader.ReadToEnd();

				timedOut = !proc.WaitForExit(timeout);

				if (!timedOut)
					exitCode = proc.ExitCode;
				else
					proc.Kill();
			}

			if (timedOut)
				throw new TaskTimeoutException($"Task {Name} timed out.");

			if (exitCode != 0)
				throw new TaskRunFailure(stdErr, stdOut, exitCode, $"Task {Name} failed.");
		}

		/// <summary>
		/// Generates the start info for a common process.
		/// </summary>
		/// <param name="workingDir">The working directory to be set.</param>
		/// <returns>A <see cref="ProcessStartInfo"/> instance which can be assigned to a task's <see cref="Process"/>.</returns>
		private ProcessStartInfo SetupStartInfo(DirectoryInfo workingDir)
		{
			return new ProcessStartInfo()
			{
				FileName = Command,
				Arguments = ArgsString,
				UseShellExecute = IsShellTask,
				WorkingDirectory = workingDir.FullName,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
		}
	}
}