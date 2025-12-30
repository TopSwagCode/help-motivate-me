#!/bin/bash
awslocal s3 mb s3://helpmotivateme-journals
awslocal s3api put-bucket-cors --bucket helpmotivateme-journals --cors-configuration '{
  "CORSRules": [{
    "AllowedOrigins": ["http://localhost:5173"],
    "AllowedMethods": ["GET"],
    "AllowedHeaders": ["*"]
  }]
}'
echo "S3 bucket 'helpmotivateme-journals' created with CORS configuration"
