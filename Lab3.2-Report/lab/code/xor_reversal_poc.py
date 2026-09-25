#!/usr/bin/env python3
import base64

KEY = b"my-static-secret-key-123"


def xor(data: bytes, key: bytes) -> bytes:
    return bytes(b ^ key[i % len(key)] for i, b in enumerate(data))


def encrypt(plaintext: str) -> str:
    return base64.b64encode(xor(plaintext.encode(), KEY)).decode()


def decrypt(ciphertext_b64: str) -> str:
    return xor(base64.b64decode(ciphertext_b64), KEY).decode()


if __name__ == "__main__":
    secret = "PROVIDED-DUMP-MARKER-Q9F3K2"
    ciphertext = encrypt(secret)
    recovered = decrypt(ciphertext)
    print(f"plaintext:  {secret}")
    print(f"ciphertext: {ciphertext}")
    print(f"recovered:  {recovered}")
    print(f"match: {recovered == secret}")
