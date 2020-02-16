import sys
sys.path.append('virgin-media-hub3')
import virginmedia

hub = virginmedia.Hub()
hub.login(None, "PASSWORD")
print(hub.wifi_clients)