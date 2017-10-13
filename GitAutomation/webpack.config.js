const path = require("path");
const UglifyJSPlugin = require("uglifyjs-webpack-plugin");

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
    loaders: [
      { test: /\.ts$/, loader: "ts-loader" },
      {
        test: /\.html$/,
        loaders: [
          "raw-loader",
          {
            loader: "html-minifier-loader",
            options: {
              removeComments: true,
              collapseWhitespace: true,
              conservativeCollapse: true,
              keepClosingSlash: true
            }
          }
        ]
      }
    ]
  }
};

if (process.argv.find(arg => arg === "--env.NODE_ENV=production")) {
  module.exports.devtool = "source-map";

  module.exports.plugins = (module.exports.plugins || []).concat([
    new UglifyJSPlugin({ extractComments: true })
  ]);
}
