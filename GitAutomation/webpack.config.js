const webpack = require("webpack");
const path = require("path");
const UglifyJSPlugin = require("uglifyjs-webpack-plugin");
const { rgb } = require("csx");

const hotfixColors = [
  JSON.stringify(rgb(255, 115, 59).toHexString()),
  JSON.stringify(rgb(255, 174, 141).toHexString()),
  JSON.stringify(rgb(255, 142, 97).toHexString()),
  JSON.stringify(rgb(255, 84, 15).toHexString()),
  JSON.stringify(rgb(214, 61, 0).toHexString())
];

const featureColors = [
  JSON.stringify(rgb(55, 127, 192).toHexString()),
  JSON.stringify(rgb(132, 181, 225).toHexString()),
  JSON.stringify(rgb(88, 151, 207).toHexString()),
  JSON.stringify(rgb(23, 105, 178).toHexString()),
  JSON.stringify(rgb(9, 77, 139).toHexString())
];

const releaseCandidateColors = [
  JSON.stringify(rgb(111, 37, 111).toHexString()),
  JSON.stringify(rgb(166, 111, 166).toHexString()),
  JSON.stringify(rgb(138, 69, 138).toHexString()),
  JSON.stringify(rgb(83, 14, 83).toHexString()),
  JSON.stringify(rgb(55, 0, 55).toHexString())
];

const serviceLineColors = [
  JSON.stringify(rgb(111, 206, 31).toHexString()),
  JSON.stringify(rgb(166, 233, 110).toHexString()),
  JSON.stringify(rgb(137, 219, 70).toHexString()),
  JSON.stringify(rgb(82, 167, 12).toHexString()),
  JSON.stringify(rgb(60, 132, 0).toHexString())
];

const integrationBranchColors = [
  JSON.stringify(rgb(98, 98, 98).toHexString()),
  JSON.stringify(rgb(127, 127, 127).toHexString()),
  JSON.stringify(rgb(112, 112, 112).toHexString()),
  JSON.stringify(rgb(83, 83, 83).toHexString()),
  JSON.stringify(rgb(67, 67, 67).toHexString())
];

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
          "html-loader",
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
      },
      {
        test: /\.woff(2)$/,
        use: [
          "url-loader?limit=10000&mimetype=application/font-woff&hash=sha512&digest=hex&name=[name].[hash].[ext]"
        ]
      },
      {
        test: /\.(ttf|eot)$/,
        use: [
          "url-loader?limit=10000&hash=sha512&digest=hex&name=[name].[hash].[ext]"
        ]
      },
      {
        test: /\.svg(\?.*)?$/,
        use: [
          "url-loader?limit=10000&hash=sha512&digest=hex&name=[name].[hash].[ext]",
          "svg-fill-loader"
        ]
      }
    ]
  },
  plugins: [
    new webpack.DefinePlugin({
      hotfixColors,
      featureColors,
      releaseCandidateColors,
      integrationBranchColors,
      serviceLineColors
    })
  ]
};

if (process.argv.find(arg => arg === "--env.NODE_ENV=production")) {
  module.exports.devtool = "";

  module.exports.plugins = (module.exports.plugins || []).concat([
    new UglifyJSPlugin({ extractComments: true })
  ]);
}
