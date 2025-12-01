import os
import time
import random
import requests
import logging
from requests.auth import HTTPBasicAuth
from flask import Flask, jsonify
from datetime import datetime

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)

RABBIT_HOST = os.environ.get('RABBIT_HOST', 'rabbitmq')
RABBIT_PORT = os.environ.get('RABBIT_PORT', '15672')
RABBIT_USER = os.environ.get('RABBIT_USER', 'guest')
RABBIT_PASS = os.environ.get('RABBIT_PASS', 'guest')
INTERVAL = int(os.environ.get('INTERVAL', '30'))

# Statistics tracking
stats = {
    'start_time': datetime.now().isoformat(),
    'total_executions': 0,
    'kill_connection': {'count': 0, 'errors': 0},
    'kill_channel': {'count': 0, 'errors': 0},
    'introduce_delay': {'count': 0, 'errors': 0},
    'last_execution': None,
    'last_error': None,
}

app = Flask(__name__)
BASE_URL = f"http://{RABBIT_HOST}:{RABBIT_PORT}/api"


@app.route("/health")
def health_check():
    return "OK", 200


@app.route("/stats")
def get_stats():
    return jsonify(stats), 200


def get_auth():
    return HTTPBasicAuth(RABBIT_USER, RABBIT_PASS)


def kill_random_connection():
    url = f"{BASE_URL}/connections"
    resp = requests.get(url, auth=get_auth())
    resp.raise_for_status()
    connections = resp.json()
    if not connections:
        logging.info("No connections found.")
        return
    victim = random.choice(connections)
    name = victim['name']
    logging.warning(f"Killing connection: {name}")
    del_url = f"{url}/{name}"
    requests.delete(del_url, auth=get_auth())


def kill_random_channel():
    url = f"{BASE_URL}/channels"
    resp = requests.get(url, auth=get_auth())
    resp.raise_for_status()
    channels = resp.json()
    if not channels:
        logging.info("No channels found.")
        return
    victim = random.choice(channels)
    name = victim['name']
    logging.warning(f"Killing channel: {name}")
    del_url = f"{url}/{name}"
    requests.delete(del_url, auth=get_auth())


def execute_random_chaos():
    strategies = [
        ("kill_connection", kill_random_connection),
        ("kill_channel", kill_random_channel),
    ]
    strategy_name, strategy_func = random.choice(strategies)
    logging.info(f"Executing chaos strategy: {strategy_name}")

    try:
        strategy_func()
        stats[strategy_name]['count'] += 1
        stats['total_executions'] += 1
        stats['last_execution'] = {
            'strategy': strategy_name,
            'timestamp': datetime.now().isoformat(),
            'status': 'success'
        }
    except Exception as e:
        logging.error(f"Error executing {strategy_name}: {e}")
        stats[strategy_name]['errors'] += 1
        stats['last_error'] = {
            'strategy': strategy_name,
            'timestamp': datetime.now().isoformat(),
            'error': str(e)
        }


def chaos_loop():
    while True:
        try:
            execute_random_chaos()
        except Exception as e:
            logging.error(f"Error: {e}")
        time.sleep(INTERVAL)


if __name__ == "__main__":
    import threading
    threading.Thread(target=chaos_loop, daemon=True).start()
    app.run(host="0.0.0.0", port=8080)
