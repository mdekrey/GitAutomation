const path = require("path");

module.exports = {
  devtool: "cheap-source-map",
  entry: "./web-scripts/index.ts",
  output: {
    path: path.resolve(__dirname, "wwwroot"),
    filename: "bundle.js",
    publicPath: "/wwwroot/"
  },
  resolve: {
    // Add `.ts` as a resolvable extension.
    extensions: [".webpack.js", ".web.js", ".ts", ".js"]
  },
  module: {
    loaders: [{ test: /\.ts$/, loader: "ts-loader" }]
  }
};
