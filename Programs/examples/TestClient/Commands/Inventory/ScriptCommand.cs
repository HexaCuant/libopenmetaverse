using System;
using System.IO;
using System.Collections.Generic;
using OpenMetaverse;

namespace OpenMetaverse.TestClient
{
    /// <summary>
    /// Reads TestClient commands from a file or named pipe (FIFO).
    /// One command per line, arguments separated by spaces.
    /// Usage: script [filename] | script |pipe_path (for named pipes)
    ///
    /// For named pipes, prefix the path with '|' to distinguish from regular files.
    /// Example: script |/tmp/bot_commands.pipe
    ///
    /// For continuous reading from a pipe, use 'scriptpipe' instead.
    /// </summary>
    public class ScriptCommand : Command
    {
        public ScriptCommand(TestClient testClient)
        {
            Name = "script";
            Description = "Reads TestClient commands from a file or named pipe. One command per line. " +
                          "Usage: script [filename] | script |pipe_path. " +
                          "For continuous pipe reading, use 'scriptpipe' command.";
            Category = CommandCategory.TestClient;
        }

        public override string Execute(string[] args, UUID fromAgentID)
        {
            if (args.Length != 1)
                return "Usage: script [filename] | script |pipe_path";

            string path = args[0];
            string[] lines;

            try
            {
                // Check if this is a named pipe (prefixed with |)
                if (path.StartsWith("|"))
                {
                    lines = ReadFromPipe(path.TrimStart('|'));
                }
                else
                {
                    // Original behavior: read from file
                    lines = File.ReadAllLines(path);
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }

            // Execute all of the commands
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (line.Length > 0)
                    ClientManager.Instance.DoCommandAll(line, UUID.Zero);
            }

            return "Finished executing " + lines.Length + " commands";
        }

        private string[] ReadFromPipe(string pipePath)
        {
            var commands = new List<string>();

            try
            {
                // Open the pipe and read all lines until the writer closes it
                using (var stream = new FileStream(pipePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        commands.Add(line);
                    }
                }
            }
            catch (IOException)
            {
                // Pipe closed unexpectedly
                throw new IOException($"Named pipe '{pipePath}' was closed before reading completed. " +
                                      $"Use 'scriptpipe' for continuous reading from a pipe.");
            }

            return commands.ToArray();
        }
    }
}

