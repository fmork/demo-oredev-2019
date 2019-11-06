# demo-oredev-2019

This is a demo web site showcasing an example of running a .NET Core application, written in C# (and some TypeScript), based on serverless services in AWS.

The site is a very simple photo community, consisting of three AWS Lambdas:

- One for the ASP.NET MVC site itself.
- One that picks up photos that are uploaded to an S3 bucket, and creates web-scale image files.
- One that calculates a "popularity score" on photos, based on likes and comments (and time).

It makes use of the following AWS services:

- [Lambda](https://aws.amazon.com/lambda/) - for hosting and running the site.</li>
- [API Gateway](https://aws.amazon.com/api-gateway/) - for exposing the site to the internet.</li>
- [DynamoDB](https://aws.amazon.com/dynamodb/) - for data storage</li>
- [Cognito](https://aws.amazon.com/cognito/) - for user authentication</li>
- [S3](https://aws.amazon.com/s3/) - for hosting photos</li>
- [CloudFront](https://aws.amazon.com/cloudfront/) - for distributing static content (mainly the photos) to the internet</li>
