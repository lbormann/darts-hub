#!/bin/bash

sleep 2
cp -R -p ./updates/* .
rm -rf ./updates
chmod +x autodarts-desktop
./autodarts-desktop
