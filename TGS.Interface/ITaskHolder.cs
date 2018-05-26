using System;
using System.Collections.Generic;
using System.IO;

namespace TGS.Interface
{
	/// <summary>
	/// An exception describing the failure of a post-merge task.
	/// </summary>
	public class TaskRunFailure : Exception
	{
		/// <summary>
		/// The STDERR of the failed task.
		/// </summary>
		public string StandardError { get; }

		/// <summary>
		/// The STDOUT of the failed task. In case of tasks which don't print to STDERR.
		/// </summary>
		public string StandardOut { get; }

		/// <summary>
		/// Exit code of the failed task. Should always be non-0.
		/// </summary>
		public int ExitCode { get; }

		public TaskRunFailure(string message)
			: base(message)
		{
		}

		public TaskRunFailure(string stdErr, string stdOut, int exitCode)
		{
			StandardError = stdErr;
			StandardOut = stdOut;
			ExitCode = exitCode;
		}

		public TaskRunFailure(string stdErr, string stdOut, int exitCode, string message)
			: base(message)
		{
			StandardError = stdErr;
			StandardOut = stdOut;
			ExitCode = exitCode;
		}

		public TaskRunFailure(string stdErr, string stdOut, int exitCode, string message, Exception inner)
			: base(message, inner)
		{
			StandardError = stdErr;
			StandardOut = stdOut;
			ExitCode = exitCode;
		}
	}

	/// <summary>
	/// Thrown when a given task times out completely and is killed.
	/// </summary>
	public class TaskTimeoutException : Exception
	{
		public TaskTimeoutException()
		{
		}

		public TaskTimeoutException(string message)
			: base(message)
		{
		}

		public TaskTimeoutException(string message, Exception inner)
			: base(message, inner)
		{
		}
	}

	/// <summary>
	/// An <see langword="interface"/> for describing a generic command-line task to
	/// be ran after a merge, before synchronization.
	/// </summary>
	public interface ITaskHolder
	{
		/// <summary>
		/// A human-readable identifier for the task. Just in case command names overlap,
		/// but arguments and functionality differ.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// The command which this task executes.
		/// </summary>
		string Command { get; }

		/// <summary>
		/// The arguments with which to execute this task.
		/// </summary>
		IList<string> ArgsList { get; }

		/// <summary>
		/// Describes whether or not the task is a shell task.
		/// </summary>
		bool IsShellTask { get; }

		/// <summary>
		/// Executes the task, running it as a new, blocking process.
		/// Make sure to lock the repo before executing this! Not thread-safe.
		/// </summary>
		/// <param name="workingDir">The repository's directory reference.</param>
		/// <param name="timeout">Time in milliseconds before timeout. By default, set to <see langword="Int32.MaxValue"/> for no timeout.</param>
		/// <exception cref="TaskRunFailure">Thrown upon task execution failure. Including bad return code.</exception>
		/// <exception cref="TaskTimeoutException">Thrown upon task time-out.</exception>
		void RunTask(DirectoryInfo workingDir, int timeout);
	}
}