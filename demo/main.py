import requests
import time
import threading
import matplotlib.pyplot as plt
from collections import defaultdict, deque
from matplotlib.animation import FuncAnimation

# URLs to send requests to
url = "http://52.226.1.10/api/loadtest?n=10"
cpu_url = "http://52.226.1.10/.metrics/cpu"

# Dictionary to store the counts of requests in the past 1 second
request_counts = defaultdict(lambda: {'true': 0, 'false': 0})

# Dictionary to store the latest CPU usage for each pod
cpu_usage = defaultdict(float)

# Deque to store the timestamps of requests for each pod and proxied status
request_timestamps = defaultdict(lambda: {'true': deque(), 'false': deque()})

# Lock for thread-safe updates to request_counts and request_timestamps
lock = threading.Lock()

def send_requests():
    while True:
        for i in range(10):
            response = requests.get(url)
            pod_name = 'test_pod' if i % 2 == 0 else 'test_pod_2'  # response.headers.get('x-pod-name')
            was_proxied = i % 3 == 0  # response.headers.get('x-was-proxied')
            with lock:
                current_time = time.time()
                request_timestamps[pod_name][str(was_proxied).lower()].append(current_time)
        time.sleep(1)

def send_cpu_requests():
    while True:
        for i in range(5):
            response = requests.get(cpu_url)
            data = response.json()
            pod_name = 'test_pod' if i % 2 == 0 else 'test_pod_2' #response.headers.get('x-pod-name')
            if pod_name:
                with lock:
                    cpu_usage[pod_name] = data['cpuUsagePercentage']
        time.sleep(1)

def update_chart(frame):
    plt.clf()
    current_time = time.time()
    
    with lock:
        # Update request_counts based on the timestamps in the past 1 second
        for pod_name in request_timestamps:
            for proxied_status in ['true', 'false']:
                # Remove timestamps older than 1 second
                while request_timestamps[pod_name][proxied_status] and current_time - request_timestamps[pod_name][proxied_status][0] > 1:
                    request_timestamps[pod_name][proxied_status].popleft()
                # Update the count of requests in the past 1 second
                request_counts[pod_name][proxied_status] = len(request_timestamps[pod_name][proxied_status])
    
    pod_names = list(request_counts.keys())
    true_counts = [request_counts[pod]['true'] for pod in pod_names]
    false_counts = [request_counts[pod]['false'] for pod in pod_names]
    
    bar_width = 0.35
    index = range(len(pod_names))
    
    plt.subplot(2, 1, 1)
    plt.bar(index, true_counts, bar_width, label='Proxied=True', color='r')
    plt.bar(index, false_counts, bar_width, bottom=true_counts, label='Proxied=False', color='b')
    
    plt.xlabel('Pod Name')
    plt.ylabel('Number of Requests')
    plt.title('Requests per Pod (Past 1 Second)')
    plt.xticks(index, pod_names)
    plt.legend()

    plt.subplot(2, 1, 2)
    cpu_values = [cpu_usage[pod] for pod in pod_names]
    
    plt.bar(index, cpu_values, bar_width, color='g')
    
    plt.xlabel('Pod Name')
    plt.ylabel('CPU Usage')
    plt.title('CPU Usage per Pod')
    plt.xticks(index, pod_names)

# Start the threads to send requests
threading.Thread(target=send_requests, daemon=True).start()
threading.Thread(target=send_cpu_requests, daemon=True).start()

# Set up the plot
fig = plt.figure()
ani = FuncAnimation(fig, update_chart, interval=1000)

plt.show()