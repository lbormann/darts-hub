#!/bin/bash

sleep 3
cp -R -p ./updates/* .
rm -rf ./updates
chmod +x darts-hub
./darts-hub
