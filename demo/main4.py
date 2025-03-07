import requests
import concurrent.futures
import time

# Define the endpoint
endpoint = "http://64.236.86.233/api/loadtest?n=2"

# Function to send a single request
def send_request():
    try:
        response = requests.get(endpoint)
        response.raise_for_status()
        return response.status_code
    except requests.exceptions.RequestException as e:
        print(f"HTTP error occurred: {e}")
        return None

# Function to send multiple requests in parallel
def send_requests_parallel(rps):
    with concurrent.futures.ThreadPoolExecutor(max_workers=rps) as executor:
        futures = [executor.submit(send_request) for _ in range(rps)]
        for future in concurrent.futures.as_completed(futures):
            status_code = future.result()
            if status_code:
                print(f"Request completed with status code: {status_code}")

# Run the script in a loop
while True:
    send_requests_parallel(20)
    time.sleep(1)