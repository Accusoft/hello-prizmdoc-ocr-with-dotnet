# Hello PrizmDoc OCR with Dotnet

A minimal sample using PrizmDoc with Docker and Dotnet to OCR images.

## Requirements

- Docker
- Dotnet 6.0+

## Running the Sample

To start the necessary containers:

```bash
docker compose up -d
```

Wait 2 minutes for the PrizmDoc container to start all the services it needs.

Then, start the C# sample to OCR an image:

```bash
dotnet run pathToYourImage.png
```
