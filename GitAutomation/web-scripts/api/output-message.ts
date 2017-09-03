export enum OutputMessageChannel {
  StartInfo = 0,
  Out = 1,
  Error = 2,
  ExitCode = 3
}

export interface OutputMessage {
  channel: OutputMessageChannel;
  message: string;
  exitCode: number;
}
