# クライアントを作成

import socket
import struct #数値→バイト列変換用
import time

with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
    # サーバを指定
    s.connect(('127.0.0.1', 60000))
    # サーバにメッセージを送る
    s.send(b'hello\n')
    # サーバからメッセージを受け取る
    s.settimeout(5)
    data = s.recv(128)
    #
    print(repr(data))
