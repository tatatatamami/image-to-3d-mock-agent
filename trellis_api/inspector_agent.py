import asyncio
import os
from typing import Annotated

import urllib.request
from agent_framework.foundry import FoundryChatClient
from agent_framework_foundry_hosting import ResponsesHostServer
from azure.identity.aio import AzureCliCredential
from dotenv import load_dotenv


def get_service_status(
    endpoint: Annotated[str, "API endpoint root such as http://127.0.0.1:8000"],
) -> str:
    """Check whether the local TRELLIS FastAPI service is reachable."""
    url = endpoint.rstrip("/") + "/health"
    try:
        with urllib.request.urlopen(url, timeout=10) as response:  # noqa: S310
            payload = response.read().decode("utf-8", "ignore")
            return f"health check succeeded: {payload}"
    except Exception as exc:  # noqa: BLE001
        return f"health check failed for {url}: {exc}"


def get_quality_steps(
    quality: Annotated[str, "One of preview, standard, high"],
) -> str:
    """Return the sampling step count used by the TRELLIS API quality option."""
    mapping = {
        "preview": 12,
        "standard": 25,
        "high": 50,
    }
    key = quality.lower().strip()
    if key not in mapping:
        return "Unknown quality. Use preview, standard, or high."
    return f"{key} uses {mapping[key]} sampling steps."


async def run_server() -> None:
    load_dotenv(override=False)

    project_endpoint = os.getenv("FOUNDRY_PROJECT_ENDPOINT")
    deployment = os.getenv("FOUNDRY_MODEL_DEPLOYMENT_NAME")

    if not project_endpoint or not deployment:
        raise RuntimeError(
            "Missing Foundry settings. Set FOUNDRY_PROJECT_ENDPOINT and "
            "FOUNDRY_MODEL_DEPLOYMENT_NAME in environment variables or .env."
        )

    async with (
        AzureCliCredential() as credential,
        FoundryChatClient(
            project_endpoint=project_endpoint,
            model=deployment,
            credential=credential,
        ).as_agent(
            name="trellis-inspector-agent",
            instructions=(
                "You are the debug assistant for the image-to-3d mock project. "
                "Use tools when users ask about API health or quality settings."
            ),
            tools=[get_service_status, get_quality_steps],
        ) as agent,
    ):
        await ResponsesHostServer(agent).run_async()


if __name__ == "__main__":
    asyncio.run(run_server())
