export enum OutputMessageChannel {
  StartInfo = "StartInfo",
  Out = "Out",
  Error = "Error",
  ExitCode = "ExitCode"
}

export interface OutputMessage {
  channel: OutputMessageChannel;
  message: string;
  exitCode: number;
}
