#!/usr/bin/env python3
"""
Code-review PoC for Lab 3.2: the "encryption" in the provided algorithm
(Lab3.2-ProvidedAlgorithm/Program.cs) is XOR with a key hardcoded in
source ("my-static-secret-key-123"), reused as-is here -- no access to
the running server or its process memory is needed, only the source
code, which is exactly what a code-security review has by definition.
"""
import base64

KEY = b"my-static-secret-key-123"  # copied verbatim from Program.cs


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
