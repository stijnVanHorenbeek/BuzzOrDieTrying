import os
import time
import random
import requests
import logging
from requests.auth import HTTPBasicAuth
from flask import Flask

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)

RABBIT_HOST = os.environ.get('RABBIT_HOST', 'rabbitmq')
RABBIT_PORT = os.environ.get('RABBIT_PORT', '15672')
RABBIT_USER = os.environ.get('RABBIT_USER', 'guest')
RABBIT_PASS = os.environ.get('RABBIT_PASS', 'guest')
INTERVAL = int(os.environ.get('INTERVAL', '10'))

app = Flask(__name__)


@app.route("/health")
def health_check():
    return "OK", 200


def kill_random_connection():
    url = f"http://{RABBIT_HOST}:{RABBIT_PORT}/api/connections"
    resp = requests.get(url, auth=HTTPBasicAuth(RABBIT_USER, RABBIT_PASS))
    resp.raise_for_status()
    connections = resp.json()
    if not connections:
        logging.info("No connections found.")
        return
    victim = random.choice(connections)
    name = victim['name']
    logging.warning(f"Killing connection: {name}")
    del_url = f"{url}/{name}"
    requests.delete(del_url, auth=HTTPBasicAuth(RABBIT_USER, RABBIT_PASS))


def chaos_loop():
    while True:
        try:
            kill_random_connection()
        except Exception as e:
            logging.error(f"Error: {e}")
        time.sleep(INTERVAL)


if __name__ == "__main__":
    import threading
    threading.Thread(target=chaos_loop, daemon=True).start()
    app.run(host="0.0.0.0", port=80)
